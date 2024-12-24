using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class HideTerminalCommand : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.HideTerminalCommand;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetTerminalFromZone(e, e.HideTerminalCommand.TerminalIndex, out var term)) return;

        TERM_Command c_num = (TERM_Command)(50 + e.HideTerminalCommand.CommandNumber);
        TERM_Command command = e.HideTerminalCommand.CommandNumber switch
        {
            1 => TERM_Command.UniqueCommand1,
            2 => TERM_Command.UniqueCommand2,
            3 => TERM_Command.UniqueCommand3,
            4 => TERM_Command.UniqueCommand4,
            5 => TERM_Command.UniqueCommand5,
            > 5 when term.m_command.m_commandsPerEnum.ContainsKey(c_num) => c_num,
            0 => e.HideTerminalCommand.CommandEnum,
            _ => TERM_Command.None
        };

        if (command != TERM_Command.None)
        {
            var state = term.m_stateReplicator.State;
            state.TryHideCommand(command);

            if (e.HideTerminalCommand.DeleteCommand)
            {
                term.TrySyncSetCommandRule(command, TERM_CommandRule.OnlyOnceDelete);
                term.TrySyncSetCommandIsUsed(command);
            }

            term.m_stateReplicator.State = state;
            //LogDebug($"Command {command} should be {(!e.HideTerminalCommand.DeleteCommand ? "hidden" : "deleted")} now");
        }
    }
}
