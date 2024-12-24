using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class UnhideTerminalCommand : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.UnhideTerminalCommand;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetTerminalFromZone(e, e.UnhideTerminalCommand.TerminalIndex, out var term)) return;

        TERM_Command c_num = (TERM_Command)(50 + e.UnhideTerminalCommand.CommandNumber);
        TERM_Command command = e.UnhideTerminalCommand.CommandNumber switch
        {
            1 => TERM_Command.UniqueCommand1,
            2 => TERM_Command.UniqueCommand2,
            3 => TERM_Command.UniqueCommand3,
            4 => TERM_Command.UniqueCommand4,
            5 => TERM_Command.UniqueCommand5,
            > 5 when term.m_command.m_commandsPerEnum.ContainsKey(c_num) => c_num,
            0 => e.UnhideTerminalCommand.CommandEnum,
            _ => TERM_Command.None
        };

        if (command != TERM_Command.None)
        {
            term.TrySyncSetCommandShow(command);
            //LogDebug($"Command {command} should be visible now");
        }
    }
}
