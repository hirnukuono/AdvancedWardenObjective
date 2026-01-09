using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class UnhideTerminalCommand : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.UnhideTerminalCommand;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerMaster(WEE_EventData e)
    {
        foreach (var unhidecmd in e.UnhideTerminalCommand.Values)
        {
            if (!TryGetTerminalFromZone(e, unhidecmd.TerminalIndex, out var term)) continue;

            TERM_Command c_num = (TERM_Command)(50 + unhidecmd.CommandNumber);
            TERM_Command command;

            if (unhidecmd.CommandNumber == 0 && unhidecmd.CommandEnum != TERM_Command.None)
            {
                command = unhidecmd.CommandEnum;
            }
            else if (term.m_command.m_commandsPerEnum.ContainsKey(c_num))
            {
                command = c_num;
            }
            else
            {
                LogError($"No TERM_Command given, or (num {unhidecmd.CommandNumber} -- enum {(int)unhidecmd.CommandEnum}) does not exist on terminal!");
                continue;
            }

            term.TrySyncSetCommandShow(command);
        }
    }
}
