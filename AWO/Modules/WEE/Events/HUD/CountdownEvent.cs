using AWO.Modules.TerminalSerialLookup;
using GameData;
using GTFO.API.Extensions;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class CountdownEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.Countdown;

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.Coroutines.CountdownStarted = Time.realtimeSinceStartup;
        EntryPoint.TimerMods.TimeModifier = 0.0f;
        float duration = ResolveFieldsFallback(e.Duration, e.Countdown.Duration);
        CoroutineManager.StartCoroutine(DoCountdown(e.Countdown, duration).WrapToIl2Cpp());
    }

    static IEnumerator DoCountdown(WEE_CountdownData cd, float duration)
    {
        int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float startTime = EntryPoint.Coroutines.CountdownStarted;
        float time = 0.0f;

        Queue<EventsOnTimerProgress> cachedProgressEvents = new(cd.EventsOnProgress);
        bool hasProgressEvents = cachedProgressEvents.Count > 0;
        double nextProgress = hasProgressEvents ? cachedProgressEvents.Peek().Progress : double.NaN;

        GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(true, true);
        GuiManager.PlayerLayer.m_objectiveTimer.SetTimerTextEnabled(true);
        GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerTitle(SerialLookupManager.ParseTextFragments(cd.TimerText));
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
                if (nextProgress == Math.Round(time / duration, 3))
                {
                    WOManager.CheckAndExecuteEventsOnTrigger(cachedProgressEvents.Dequeue().Events.ToIl2Cpp(), eWardenObjectiveEventTrigger.None);
                    if ((hasProgressEvents = cachedProgressEvents.Count > 0) == true)
                    {
                        nextProgress = Math.Round(cachedProgressEvents.Peek().Progress, 3);
                    }
                }
            }

            GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerText(duration - time, duration, cd.TimerColor);
            time += Time.deltaTime;

            if (EntryPoint.TimerMods.TimeModifier != 0.0f) // time mod
            {
                time -= EntryPoint.TimerMods.TimeModifier;
                EntryPoint.TimerMods.TimeModifier = 0.0f;
            }
            if (EntryPoint.TimerMods.CountupText != cd.TimerText) // text mod
            {
                cd.TimerText = EntryPoint.TimerMods.CountupText;
                GuiManager.PlayerLayer.m_objectiveTimer.m_timerText.text = SerialLookupManager.ParseTextFragments(cd.TimerText);
            }
            if (EntryPoint.TimerMods.TimerColor != cd.TimerColor) // color mod
            {
                cd.TimerColor = EntryPoint.TimerMods.TimerColor;
            }

            yield return null;
        }

        GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(false, true);
        WOManager.CheckAndExecuteEventsOnTrigger(cd.EventsOnDone.ToIl2Cpp(), eWardenObjectiveEventTrigger.None);
    }
}
