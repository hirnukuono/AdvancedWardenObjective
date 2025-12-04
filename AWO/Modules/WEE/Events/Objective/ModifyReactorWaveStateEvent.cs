using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class ModifyReactorWaveStateEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ModifyReactorWaveState;

    protected override void TriggerMaster(WEE_EventData e)
    {
        foreach (var keyValue in WOManager.Current.m_wardenObjectiveItem)
        {
            if (keyValue.Key.Layer != e.Layer) continue;
            var reactor = keyValue.Value.TryCast<LG_WardenObjective_Reactor>();
            if (reactor == null) continue;

            var state = reactor.m_stateReplicator.State;

            if (e.Reactor.State == WEE_ReactorEventData.WaveState.Idle && state.status != eReactorStatus.Inactive_Idle)
            {
                state.stateCount = 0;
                state.verifyFailed = false;
                reactor.AttemptInteract(eReactorInteraction.Goto_inactive);
                reactor.SetGUIMessage(true, string.Empty);
                GuiManager.InteractionLayer.MessageVisible = false;
                GuiManager.InteractionLayer.MessageTimerVisible = false;
            }
            else if (state.status is eReactorStatus.Startup_intro or eReactorStatus.Startup_intense or eReactorStatus.Startup_waitForVerify or eReactorStatus.Startup_complete)
            {
                // only change wave number if it is different from -1
                // should be ok for existing mdos that use this.
                if (e.Reactor.Wave != -1)
                    state.stateCount = e.Reactor.Wave;

                // only change duration if different from -1
                if (e.Reactor.Duration != null && e.Reactor.Duration > 0)
                {
                    // calculate how many seconds passed to make sure seconds passed remains the exact same
                    // notice this change is done prior to the e.Reactor.Progress check which means it does nothing
                    // if e.Reactor.Progress != -1
                    var secondsPassed = reactor.m_currentDuration * state.stateProgress;
                    var newPercent = secondsPassed / e.Reactor.Duration;
                    // updates the total timer so that:
                    // 1. the timer from now progresses slower.
                    // 2. the bar at the top updates properly.
                    reactor.m_currentDuration = (float) e.Reactor.Duration;
                    state.stateProgress = (float) newPercent;
                }

                // only change progress if different from -1
                // this should be OK? for existing mods that use this.
                if (e.Reactor.Progress != -1)
                    state.stateProgress = e.Reactor.Progress;

                state.verifyFailed = false;
                state.status = e.Reactor.State switch
                {
                    WEE_ReactorEventData.WaveState.Intro => eReactorStatus.Startup_intro,
                    WEE_ReactorEventData.WaveState.Wave => eReactorStatus.Startup_intense,
                    WEE_ReactorEventData.WaveState.Verify => eReactorStatus.Startup_waitForVerify,
                    _ => eReactorStatus.Startup_intro
                };

                reactor.m_stateReplicator.State = state;
            }
            else
            {
                LogError("Reactor in invalid state, or already Inactive_Idle");
            }
        }
    }
}
