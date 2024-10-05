using AWO.Modules.WEE;
using LevelGeneration;
using SNetwork;

namespace AWO.WEE.Events.Terminal;

internal sealed class UnhideTerminalCommand : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.UnhideTerminalCommand;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone))
        {
            LogError("Zone is missing?");
            return;
        }

        var term = zone.TerminalsSpawnedInZone[e.UnhideTerminalCommand.TerminalIndex];
        TERM_Command tempcommand = TERM_Command.None;
        if (e.UnhideTerminalCommand.CommandNumber == 1) tempcommand = TERM_Command.UniqueCommand1;
        if (e.UnhideTerminalCommand.CommandNumber == 2) tempcommand = TERM_Command.UniqueCommand2;
        if (e.UnhideTerminalCommand.CommandNumber == 3) tempcommand = TERM_Command.UniqueCommand3;
        if (e.UnhideTerminalCommand.CommandNumber == 4) tempcommand = TERM_Command.UniqueCommand4;
        if (e.UnhideTerminalCommand.CommandNumber == 5) tempcommand = TERM_Command.UniqueCommand5;
        if (e.UnhideTerminalCommand.CommandNumber > 5)
            if (term.m_command.m_commandsPerEnum.ContainsKey((TERM_Command)(50 + e.UnhideTerminalCommand.CommandNumber)))
                tempcommand = (TERM_Command)(50 + e.UnhideTerminalCommand.CommandNumber);

        if (e.UnhideTerminalCommand.CommandEnum != 0) tempcommand = e.UnhideTerminalCommand.CommandEnum;

        if (SNet.IsMaster && tempcommand != TERM_Command.None)
        {
            term.TrySyncSetCommandShow(tempcommand);
            // if (SNet.IsMaster) if (e.WardenIntel != "") WorldEventManager.ExecuteEvent(new() { Type = 0, WardenIntel = e.WardenIntel });
            LogDebug($"Command {tempcommand} should be visible now");
        }
    }
}
