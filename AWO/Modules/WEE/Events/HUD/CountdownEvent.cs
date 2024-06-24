﻿using AWO.Modules.WEE;
using GTFO.API.Utilities;
using System.Collections;
using UnityEngine;
using SNetwork;

namespace AWO.WEE.Events.HUD;

internal sealed class CountdownEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.Countdown;

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.CountdownStarted = Time.realtimeSinceStartup;
        CoroutineDispatcher.StartCoroutine(DoCountdown(e.Countdown, GetDuration(e)));
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
        int myreloadcount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float mystarttime = EntryPoint.CountdownStarted;
        var time = 0.0f;

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

            if (mystarttime < EntryPoint.CountdownStarted)
            {
                // someone has started a new countdown while we were stuck here, exit
                yield break;
            }

            if (CheckpointManager.Current.m_stateReplicator.State.reloadCount > myreloadcount)
            {
                // checkpoint has been used
                GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(false, false);
                GuiManager.PlayerLayer.m_objectiveTimer.SetTimerTextEnabled(false);
                yield break;
            }

            GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerText(duration - time, duration, cd.TimerColor);
            time += Time.deltaTime;
            
            if (EntryPoint.TimerMods.TimeModifier != 0.0f)
            {
                time -= EntryPoint.TimerMods.TimeModifier;
            }

            yield return null;
        }

        GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(false, false);
        foreach (var eventData in cd.EventsOnDone)
            if (SNet.IsMaster) 
                WorldEventManager.ExecuteEvent(eventData);
    }
}
