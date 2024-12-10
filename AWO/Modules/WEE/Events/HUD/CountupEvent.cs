using AK;
using FluffyUnderware.Curvy.Utils;
using GameData;
using GTFO.API.Extensions;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class CountupEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.Countup;

    private const string Tag = "[COUNTUP]";
    private const string Max = "[CMAX]";

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.Coroutines.CountdownStarted = Time.realtimeSinceStartup;
        EntryPoint.TimerMods.TimeModifier = 0.0f;
        float duration = ResolveFieldFallback(e.Duration, e.Countup.Duration);
        CoroutineManager.StartCoroutine(DoCountup(e.Countup, duration).WrapToIl2Cpp());
    }

    static IEnumerator DoCountup(WEE_CountupData cu, float duration)
    {
        int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float startTime = EntryPoint.Coroutines.CountdownStarted;
        float count = cu.StartValue;
        string[] body = ParseCustomText(cu.CustomText, duration);

        List<EventsOnTimerProgress> cachedProgressEvents = new(cu.EventsOnProgress);
        bool hasProgressEvents = cachedProgressEvents.Count > 0;

        EntryPoint.TimerMods.SpeedModifier = cu.Speed;
        EntryPoint.TimerMods.CountupText = cu.CustomText;
        EntryPoint.TimerMods.TimerColor = cu.TimerColor;

        CoroutineManager.BlinkIn(GuiManager.PlayerLayer.m_objectiveTimer.gameObject);
        GuiManager.PlayerLayer.m_objectiveTimer.m_timerSoundPlayer.Post(EVENTS.STINGER_SUBOBJECTIVE_COMPLETE, true);
        GuiManager.PlayerLayer.m_objectiveTimer.m_titleText.text = cu.TimerText;
        yield return new WaitForSeconds(0.25f);

        while (count <= duration)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
            {
                yield break;
            }
            if (startTime < EntryPoint.Coroutines.CountdownStarted)
            {
                yield break; // someone has started a new countup while we were stuck here, exit
            }
            if (CheckpointManager.Current.m_stateReplicator.State.reloadCount > reloadCount)
            {
                GuiManager.PlayerLayer.m_objectiveTimer.gameObject.active = false;
                yield break; // checkpoint has been used
            }

            if (hasProgressEvents)
            {
                foreach (var progressEvents in cachedProgressEvents.Where(prEv => !prEv.HasBeenActivated && prEv.Progress.Approximately(count / duration)))
                {
                    WOManager.CheckAndExecuteEventsOnTrigger(progressEvents.Events.ToIl2Cpp(), eWardenObjectiveEventTrigger.None);
                    progressEvents.SetActivated();
                }
                hasProgressEvents = cachedProgressEvents.Any(prEv => !prEv.HasBeenActivated);
            }

            GuiManager.PlayerLayer.m_objectiveTimer.m_timerText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(cu.TimerColor)}>{body[0]}{count.ToString($"F{cu.DecimalPoints}")}{body[1]}</color>";
            count += cu.Speed * Time.deltaTime;
            ModifyCountup(ref cu, ref count, duration, ref body);

            yield return null;
        }

        CoroutineManager.BlinkOut(GuiManager.PlayerLayer.m_objectiveTimer.gameObject);
        GuiManager.PlayerLayer.m_objectiveTimer.m_timerSoundPlayer.Post(EVENTS.STINGER_SUBOBJECTIVE_COMPLETE, true);
        WOManager.CheckAndExecuteEventsOnTrigger(cu.EventsOnDone.ToIl2Cpp(), eWardenObjectiveEventTrigger.None);
    }

    private static string[] ParseCustomText(string custom, float duration)
    {
        if (!custom.Contains(Tag))
            return new string[] { string.Empty, string.Empty };

        if (custom.Contains(Max))
            custom = custom.Replace(Max, duration.ToString());

        return custom.Split(Tag, 2);
    }

    private static void ModifyCountup(ref WEE_CountupData cu, ref float count, float duration, ref string[] body)
    {
        if (EntryPoint.TimerMods.TimeModifier != 0.0f) // time mod
        {
            count += EntryPoint.TimerMods.TimeModifier;
            EntryPoint.TimerMods.TimeModifier = 0.0f;
        }

        if (EntryPoint.TimerMods.SpeedModifier != cu.Speed) // speed mod
        {
            cu.Speed = EntryPoint.TimerMods.SpeedModifier;
        }

        if (EntryPoint.TimerMods.CountupText != cu.CustomText) // text mod
        {
            cu.CustomText = EntryPoint.TimerMods.CountupText;
            body = ParseCustomText(cu.CustomText, duration);
        }

        if (EntryPoint.TimerMods.TimerColor != cu.TimerColor) // color mod
        {
            cu.TimerColor = EntryPoint.TimerMods.TimerColor;
        }
    }
}