using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class UnhideTerminalCommand : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.UnhideTerminalCommand;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetTerminalFromZone(e, e.UnhideTerminalCommand.TerminalIndex, out var term)) return;

        TERM_Command c_num = (TERM_Command)(50 + e.UnhideTerminalCommand.CommandNumber);
        TERM_Command command;

        if (e.UnhideTerminalCommand.CommandNumber == 0 && e.UnhideTerminalCommand.CommandEnum != TERM_Command.None)
        {
            command = e.UnhideTerminalCommand.CommandEnum;
        }
        else if (term.m_command.m_commandsPerEnum.ContainsKey(c_num))
        {
            command = c_num;
        }
        else
        {
            LogError($"No TERM_Command given, or (num {e.HideTerminalCommand.CommandNumber} -- enum {(int)e.HideTerminalCommand.CommandEnum}) does not exist in terminal!");
            return;
        }
        
        term.TrySyncSetCommandShow(command);
    }
}
