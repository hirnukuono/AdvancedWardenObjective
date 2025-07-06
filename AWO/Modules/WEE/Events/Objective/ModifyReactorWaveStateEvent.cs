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
                state.stateCount = e.Reactor.Wave;
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
