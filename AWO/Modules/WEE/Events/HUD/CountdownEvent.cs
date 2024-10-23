using AWO.Modules.WEE;
using System.Collections;
using UnityEngine;

namespace AWO.WEE.Events.HUD;

internal sealed class CountdownEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.Countdown;

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.Coroutines.CountdownStarted = Time.realtimeSinceStartup;
        EntryPoint.TimerMods.TimeModifier = 0.0f;
        CoroutineManager.StartCoroutine(DoCountdown(e.Countdown, GetDuration(e)).WrapToIl2Cpp());
    }

    private static float GetDuration(WEE_EventData e)
    {
        if (e.Countdown.Duration != 0.0f)
            return e.Countdown.Duration;
        else if (e.Duration != 0.0f)
            return e.Duration;
       
        return e.Countdown.Duration;
    }

    static IEnumerator DoCountdown(WEE_CountdownData cd, float duration)
    {
        int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float startTime = EntryPoint.Coroutines.CountdownStarted;
        float time = 0.0f;

        List<EventsOnTimerProgress> cachedProgressEvents = new(cd.EventsOnProgress);
        bool hasProgressEvents = cachedProgressEvents.Count > 0;

        GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(true, true);
        GuiManager.PlayerLayer.m_objectiveTimer.SetTimerTextEnabled(true);
        GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerTitle(cd.TimerText.ToString());
        GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerText(duration - time, duration, cd.TimerColor);

        while (time <= duration)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
            {
                yield break;
            }
            if (startTime < EntryPoint.Coroutines.CountdownStarted)
            {
                // someone has started a new countdown while we were stuck here, exit
                yield break;
            }
            if (CheckpointManager.Current.m_stateReplicator.State.reloadCount > reloadCount)
            {
                // checkpoint has been used
                GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(false, false);
                GuiManager.PlayerLayer.m_objectiveTimer.SetTimerTextEnabled(false);
                yield break;
            }

            if (hasProgressEvents)
            {
                List<EventsOnTimerProgress> removeQ = new();
                foreach (var progressEvents in cachedProgressEvents.Where(x => Math.Round(x.Progress, 2) == Math.Round(time / duration, 2)))
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

            GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerText(duration - time, duration, cd.TimerColor);
            time += Time.deltaTime;

            if (EntryPoint.TimerMods.TimeModifier != 0.0f)
            {
                time -= EntryPoint.TimerMods.TimeModifier;
                EntryPoint.TimerMods.TimeModifier = 0.0f;
            }

            yield return null;
        }

        GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(false, true);
        foreach (var eventData in cd.EventsOnDone)
        {
            WorldEventManager.ExecuteEvent(eventData);
        }
    }
}
