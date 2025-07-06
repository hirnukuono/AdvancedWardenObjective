using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class HideTerminalCommand : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.HideTerminalCommand;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetTerminalFromZone(e, e.HideTerminalCommand.TerminalIndex, out var term)) return;

        TERM_Command c_num = (TERM_Command)(50 + e.HideTerminalCommand.CommandNumber);
        TERM_Command command;

        if (e.HideTerminalCommand.CommandNumber == 0 && e.HideTerminalCommand.CommandEnum != TERM_Command.None)
        {
            command = e.HideTerminalCommand.CommandEnum;
        }
        else if (term.m_command.m_commandsPerEnum.ContainsKey(c_num))
        {
            command = c_num;
        }
        else
        {
            LogError($"No TERM_Command given, or (num {e.HideTerminalCommand.CommandNumber} -- enum {(int)e.HideTerminalCommand.CommandEnum}) does not exist on terminal!");
            return;
        }

        if (e.HideTerminalCommand.DeleteCommand)
        {
            string cmdStr = term.m_command.m_commandsPerEnum[command];
            term.m_command.m_commandsPerEnum.Remove(command);
            term.m_command.m_commandsPerString.Remove(cmdStr);
            term.m_command.m_commandHelpStrings.Remove(command);
            term.m_command.m_commandEventMap.Remove(command);
            term.m_command.m_commandPostOutputMap.Remove(command);
        }
        else if (IsMaster)
        {
            var state = term.m_stateReplicator.State;
            state.TryHideCommand(command);
            term.m_stateReplicator.State = state;
        }
    }
}
