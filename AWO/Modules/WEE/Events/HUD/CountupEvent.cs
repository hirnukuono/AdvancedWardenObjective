using AK;
using AWO.Modules.WEE;
using System.Collections;
using UnityEngine;

namespace AWO.WEE.Events.HUD;

internal sealed class CountupEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.Countup;

    private const string Tag = "[COUNTUP]";
    private const string Max = "[CMAX]";

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.Coroutines.CountdownStarted = Time.realtimeSinceStartup;
        EntryPoint.TimerMods.TimeModifier = 0.0f;
        CoroutineManager.StartCoroutine(DoCountup(e.Countup, GetDuration(e)).WrapToIl2Cpp());
    }

    private static float GetDuration(WEE_EventData e)
    {
        if (e.Countup.Duration != 0.0f)
            return e.Countup.Duration;
        else if (e.Duration != 0.0f)
            return e.Duration;

        return e.Countup.Duration;
    }

    static IEnumerator DoCountup(WEE_CountupData cu, float duration)
    {
        int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float startTime = EntryPoint.Coroutines.CountdownStarted;
        float count = cu.StartValue;
        string[] body = ParseCustomText(cu.CustomText.ToString(), duration);

        List<EventsOnTimerProgress> cachedProgressEvents = new(cu.EventsOnProgress);
        bool hasProgressEvents = cachedProgressEvents.Count > 0;

        EntryPoint.TimerMods.SpeedModifier = cu.Speed;
        EntryPoint.TimerMods.CountupText = cu.CustomText;
        EntryPoint.TimerMods.TimerColor = cu.TimerColor;

        CoroutineManager.BlinkIn(GuiManager.PlayerLayer.m_objectiveTimer.gameObject);
        GuiManager.PlayerLayer.m_objectiveTimer.m_timerSoundPlayer.Post(EVENTS.STINGER_SUBOBJECTIVE_COMPLETE, true);
        GuiManager.PlayerLayer.m_objectiveTimer.m_titleText.text = cu.TimerText.ToString();
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
                List<EventsOnTimerProgress> removeQ = new();
                foreach (var progressEvents in cachedProgressEvents.Where(x => Math.Round(x.Progress, 2) == Math.Round(count / duration, 2)))
                {
                    foreach (var eventData in progressEvents.Events)
                    {
                        WorldEventManager.ExecuteEvent(eventData);
                    }
                    removeQ.Add(progressEvents);   
                }
                removeQ.ForEach(y => cachedProgressEvents.Remove(y));
                hasProgressEvents = cachedProgressEvents.Count > 0;
            }

            GuiManager.PlayerLayer.m_objectiveTimer.m_timerText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(cu.TimerColor)}>{body[0]}{count.ToString($"F{cu.DecimalPoints}")}{body[1]}</color>";
            count += cu.Speed * Time.deltaTime;
            ModifyCountup(ref cu, ref count, duration, ref body);

            yield return null;
        }

        CoroutineManager.BlinkOut(GuiManager.PlayerLayer.m_objectiveTimer.gameObject);
        GuiManager.PlayerLayer.m_objectiveTimer.m_timerSoundPlayer.Post(EVENTS.STINGER_SUBOBJECTIVE_COMPLETE, true);

        foreach (var eventData in cu.EventsOnDone)
        {
            WorldEventManager.ExecuteEvent(eventData);
        }
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
            body = ParseCustomText(cu.CustomText.ToString(), duration);
        }

        if (EntryPoint.TimerMods.TimerColor != cu.TimerColor) // color mod
        {
            cu.TimerColor = EntryPoint.TimerMods.TimerColor;
        }
    }
}