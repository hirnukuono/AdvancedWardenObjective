using AWO.Modules.WEE;
using LevelGeneration;
using SNetwork;

namespace AWO.WEE.Events.Terminal;

internal sealed class HideTerminalCommand : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.HideTerminalCommand;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone))
        {
            LogError("Zone is Missing?");
            return;
        }
        
        var term = zone.TerminalsSpawnedInZone[e.HideTerminalCommand.TerminalIndex];
        TERM_Command tempcommand = TERM_Command.None;
        if (e.HideTerminalCommand.CommandNumber == 1) tempcommand = TERM_Command.UniqueCommand1;
        if (e.HideTerminalCommand.CommandNumber == 2) tempcommand = TERM_Command.UniqueCommand2;
        if (e.HideTerminalCommand.CommandNumber == 3) tempcommand = TERM_Command.UniqueCommand3;
        if (e.HideTerminalCommand.CommandNumber == 4) tempcommand = TERM_Command.UniqueCommand4;
        if (e.HideTerminalCommand.CommandNumber == 5) tempcommand = TERM_Command.UniqueCommand5;
        if (e.HideTerminalCommand.CommandNumber > 5)
            if (term.m_command.m_commandsPerEnum.ContainsKey((TERM_Command)(50 + e.HideTerminalCommand.CommandNumber)))
                tempcommand = (TERM_Command)(50 + e.HideTerminalCommand.CommandNumber);

        if (e.HideTerminalCommand.CommandEnum != 0) tempcommand = e.HideTerminalCommand.CommandEnum;

        if (SNet.IsMaster && tempcommand != TERM_Command.None)
        {
            term.TrySyncSetCommandHidden(tempcommand);
            // if (SNet.IsMaster) if (e.WardenIntel != "") WorldEventManager.ExecuteEvent(new() { Type=0, WardenIntel=e.WardenIntel });
            UnityEngine.Debug.Log($"AdvancedWardenObjective - command {tempcommand} should be hidden now");
        }
    }
}
