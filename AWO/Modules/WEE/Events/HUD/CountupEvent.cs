using AK;
using AWO.Modules.TerminalSerialLookup;
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
        float duration = ResolveFieldsFallback(e.Duration, e.Countup.Duration);
        CoroutineManager.StartCoroutine(DoCountup(e.Countup, duration).WrapToIl2Cpp());
    }

    static IEnumerator DoCountup(WEE_CountupData cu, float duration)
    {
        int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float startTime = EntryPoint.Coroutines.CountdownStarted;
        float count = cu.StartValue;

        Queue<EventsOnTimerProgress> cachedProgressEvents = new(cu.EventsOnProgress.OrderBy(prEv => prEv.Progress));
        bool hasProgressEvents = cachedProgressEvents.Count > 0;
        double nextProgress = hasProgressEvents ? cachedProgressEvents.Peek().Progress : double.NaN;

        string titleText = SerialLookupManager.ParseTextFragments(cu.TimerText);
        string customText = SerialLookupManager.ParseTextFragments(cu.CustomText);
        string[] body = ParseCustomText(customText, duration);

        EntryPoint.TimerMods.SpeedModifier = cu.Speed;
        EntryPoint.TimerMods.TimerTitleText = new(titleText);
        EntryPoint.TimerMods.TimerBodyText = new(customText);
        EntryPoint.TimerMods.TimerColor = cu.TimerColor;

        CoroutineManager.BlinkIn(GuiManager.PlayerLayer.m_objectiveTimer.gameObject);
        GuiManager.PlayerLayer.m_objectiveTimer.m_timerSoundPlayer.Post(EVENTS.STINGER_SUBOBJECTIVE_COMPLETE, true);
        GuiManager.PlayerLayer.m_objectiveTimer.m_titleText.text = titleText;
        yield return new WaitForSeconds(0.25f);

        while (count <= duration)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || startTime < EntryPoint.Coroutines.CountdownStarted)
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
                if (nextProgress <= NormalizedPercent(count, cu.StartValue, duration))
                {
                    WOManager.CheckAndExecuteEventsOnTrigger(cachedProgressEvents.Dequeue().Events.ToIl2Cpp(), eWardenObjectiveEventTrigger.None);
                    if ((hasProgressEvents = cachedProgressEvents.Count > 0) == true)
                    {
                        nextProgress = Math.Round(cachedProgressEvents.Peek().Progress, 3);
                    }
                }
            }

            GuiManager.PlayerLayer.m_objectiveTimer.m_timerText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(cu.TimerColor)}>{body[0]}{count.ToString($"F{cu.DecimalPoints}")}{body[1]}</color>";
            count += cu.Speed * Time.deltaTime;

            if (EntryPoint.TimerMods.TimeModifier != 0.0f) // time mod
            {
                count += EntryPoint.TimerMods.TimeModifier;
                EntryPoint.TimerMods.TimeModifier = 0.0f;
            }
            if (EntryPoint.TimerMods.SpeedModifier != cu.Speed) // speed mod
            {
                cu.Speed = EntryPoint.TimerMods.SpeedModifier;
            }
            if (EntryPoint.TimerMods.TimerTitleText != titleText) // title text mod
            {
                titleText = EntryPoint.TimerMods.TimerTitleText;
                GuiManager.PlayerLayer.m_objectiveTimer.m_titleText.text = titleText;
            }
            if (EntryPoint.TimerMods.TimerBodyText != customText) // body text mod
            {
                customText = EntryPoint.TimerMods.TimerBodyText;
                body = ParseCustomText(customText, duration);
            }
            if (EntryPoint.TimerMods.TimerColor != cu.TimerColor) // color mod
            {
                cu.TimerColor = EntryPoint.TimerMods.TimerColor;
            }

            yield return null;
        }

        CoroutineManager.BlinkOut(GuiManager.PlayerLayer.m_objectiveTimer.gameObject);
        GuiManager.PlayerLayer.m_objectiveTimer.m_timerSoundPlayer.Post(EVENTS.STINGER_SUBOBJECTIVE_COMPLETE, true);
        
        while (cachedProgressEvents.Count > 0) 
        {
            WOManager.CheckAndExecuteEventsOnTrigger(cachedProgressEvents.Dequeue().Events.ToIl2Cpp(), eWardenObjectiveEventTrigger.None);
        }
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

    private static double NormalizedPercent(float current, float min, float max)
    {
        if (min == max) return double.NaN;

        float clamp = Math.Clamp(current, min, max);
        float percent = (clamp - min) / (max - min);
        return Math.Round(percent, 3);
    }
}