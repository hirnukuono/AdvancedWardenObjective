using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class StartReactorEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.StartReactor;

    protected override void TriggerMaster(WEE_EventData e)
    {
        foreach (var kvp in WOManager.Current.m_wardenObjectiveItem)
        {
            if (kvp.Key.Layer != e.Layer) continue;
            var reactor = kvp.Value.TryCast<LG_WardenObjective_Reactor>();
            if (reactor == null) continue;

            var state = reactor.m_currentState;
            if (state.status == eReactorStatus.Inactive_Idle)
            {
                reactor.OnInitialPuzzleSolved();
                reactor.m_terminal.TrySyncSetCommandIsUsed(TERM_Command.ReactorStartup);
            }
            else if (state.status == eReactorStatus.Active_Idle)
            {
                reactor.OnInitialPuzzleSolved();
                reactor.m_terminal.TrySyncSetCommandIsUsed(TERM_Command.ReactorShutdown);
            }
            else
            {
                LogError($"{Name} only works while in idle state!");
            }
        }
    }
}
