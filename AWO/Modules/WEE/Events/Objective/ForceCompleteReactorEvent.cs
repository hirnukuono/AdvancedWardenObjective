using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class ForceCompleteReactorEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ForceCompleteReactor;

    protected override void TriggerMaster(WEE_EventData e)
    {
        foreach (var keyvalue in WOManager.Current.m_wardenObjectiveItem)
        {
            if (keyvalue.Key.Layer != e.Layer) continue;
            var reactor = keyvalue.Value.TryCast<LG_WardenObjective_Reactor>();
            if (reactor == null) continue;

            var state = reactor.m_currentState;
            switch (state.status)
            {
                case eReactorStatus.Inactive_Idle:
                case eReactorStatus.Startup_intro:
                case eReactorStatus.Startup_intense:
                case eReactorStatus.Startup_waitForVerify:
                    state.status = eReactorStatus.Startup_complete;
                    reactor.m_stateReplicator.State = state;
                    break;

                case eReactorStatus.Active_Idle:
                case eReactorStatus.Shutdown_intro:
                case eReactorStatus.Shutdown_waitForVerify:
                case eReactorStatus.Shutdown_puzzleChaos:
                    state.status = eReactorStatus.Shutdown_complete;
                    reactor.m_stateReplicator.State = state;
                    break;

                case eReactorStatus.Startup_complete:
                case eReactorStatus.Shutdown_complete:
                    LogDebug("Reactor is already completed!");
                    break;
            }
        }
    }
}
