using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class ModifyReactorWaveStateEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ModifyReactorWaveState;

    protected override void TriggerMaster(WEE_EventData e)
    {
        foreach (var keyvalue in WOManager.Current.m_wardenObjectiveItem)
        {
            if (keyvalue.Key.Layer != e.Layer) continue;
            var reactor = keyvalue.Value.TryCast<LG_WardenObjective_Reactor>();
            if (reactor == null) continue;

            var state = reactor.m_stateReplicator.State;
            switch (state.status)
            {
                case eReactorStatus.Inactive_Idle:
                case eReactorStatus.Startup_complete:
                case eReactorStatus.Startup_intense:
                case eReactorStatus.Startup_intro:
                case eReactorStatus.Startup_waitForVerify:
                    state.stateCount = e.Reactor.Wave;
                    state.stateProgress = e.Reactor.Progress;
                    state.status = e.Reactor.State switch
                    {
                        WEE_ReactorEventData.WaveState.Intro => eReactorStatus.Startup_intro,
                        WEE_ReactorEventData.WaveState.Wave => eReactorStatus.Startup_intense,
                        WEE_ReactorEventData.WaveState.Verify => eReactorStatus.Startup_waitForVerify,
                        _ => eReactorStatus.Startup_intro
                    };
                    reactor.m_stateReplicator.State = state;
                    break;
            }
        }
    }
}
