using AWO.Modules.WEE;
using LevelGeneration;

namespace AWO.WEE.Events.Objective;

internal sealed class StartReactorEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.StartReactor;

    protected override void TriggerMaster(WEE_EventData e)
    {
        foreach (var keyvalue in WOManager.Current.m_wardenObjectiveItem)
        {
            if (keyvalue.Key.Layer != e.Layer) continue;
            var reactor = keyvalue.Value.TryCast<LG_WardenObjective_Reactor>();
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
                LogError($"{Name} only works while idle state!");
            }
        }
    }
}
