using AWO.Modules.WEE;
using GTFO.API.Utilities;
using System.Collections;
using UnityEngine;
using SNetwork;

namespace AWO.WEE.Events.HUD;

internal sealed class CountupEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.Countdown;

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.CountdownStarted = Time.realtimeSinceStartup;
        EntryPoint.TimerModifier = 0.0f;
        CoroutineDispatcher.StartCoroutine(DoCountupStopwatch(e));
    }

    static IEnumerator DoCountupStopwatch(WEE_EventData e)
    {
        int myReloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float myStartTime = EntryPoint.CountdownStarted;
        var cu = e.Countup;
        var duration = e.Duration;

        var time = 0.0f;

        GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(true, true);
        GuiManager.PlayerLayer.m_objectiveTimer.SetTimerTextEnabled(true);
        GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerTitle(cu.TimerText.ToString());
        GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerText(time, duration, cu.TimerColor);

        while (time <= duration)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
            {
                yield break;
            }

            if (myStartTime < EntryPoint.CountdownStarted)
            {
                // someone has started a new countup while we were stuck here, exit
                yield break;
            }

            if (CheckpointManager.Current.m_stateReplicator.State.reloadCount > myReloadCount)
            {
                // checkpoint has been used
                GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(false, false);
                GuiManager.PlayerLayer.m_objectiveTimer.SetTimerTextEnabled(false);
                yield break;
            }

            GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerText(time, duration, cu.TimerColor);
            time += Time.deltaTime;

            if (EntryPoint.TimerModifier != 0.0f)
            {
                time -= EntryPoint.TimerModifier;
                EntryPoint.TimerModifier = 0.0f;
            }

            yield return null;
        }

        GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(false, false);
        foreach (var eventData in cu.EventsOnDone)
        {
            if (SNet.IsMaster) WorldEventManager.ExecuteEvent(eventData);
        }
    }
}
