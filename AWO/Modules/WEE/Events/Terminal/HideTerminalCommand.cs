using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class HideTerminalCommand : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.HideTerminalCommand;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerCommon(WEE_EventData e)
    {
        foreach (var hidecmd in e.HideTerminalCommand.Values)
        {
            if (!TryGetTerminalFromZone(e, hidecmd.TerminalIndex, out var term)) 
                continue;

            TERM_Command c_num = (TERM_Command)(50 + hidecmd.CommandNumber);
            TERM_Command command;

            if (hidecmd.CommandNumber == 0 && hidecmd.CommandEnum != TERM_Command.None)
            {
                command = hidecmd.CommandEnum;
            }
            else if (term.m_command.m_commandsPerEnum.ContainsKey(c_num))
            {
                command = c_num;
            }
            else
            {
                LogError($"No TERM_Command given, or (num {hidecmd.CommandNumber} -- enum {(int)hidecmd.CommandEnum}) does not exist on terminal!");
                continue;
            }

            if (hidecmd.DeleteCommand)
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
}
