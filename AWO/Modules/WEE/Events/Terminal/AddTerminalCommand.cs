using AWO.Modules.WEE;
using ChainedPuzzles;
using GameData;
using LevelGeneration;
using Localization;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;

namespace AWO.WEE.Events.Terminal;

internal sealed class AddTerminalCommand : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.AddTerminalCommand;
    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone))
        {
            LogError("Zone is missing?");
            return;
        }

        var term = zone.TerminalsSpawnedInZone[e.AddTerminalCommand.TerminalIndex];
        int num = 50 + e.AddTerminalCommand.CommandNumber;
        if (term.m_command.m_commandsPerEnum.ContainsKey((TERM_Command)num))
        {
            LogError("Command with index has already been added to terminal!");
            return;
        }

        Il2CppGeneric.List<WardenObjectiveEventData> eventlist = new();
        foreach (var asd in e.AddTerminalCommand.CommandEvents) eventlist.Add(asd);

        Il2CppGeneric.List<TerminalOutput> outputlist = new();
        foreach (var asd in e.AddTerminalCommand.PostCommandOutputs) outputlist.Add(asd);

        LocalizedText desc = new() { UntranslatedText = e.AddTerminalCommand.CommandDesc.ToString() };

        term.m_command.m_commandsPerEnum.Add((TERM_Command)num, e.AddTerminalCommand.Command.ToLower());
        term.m_command.m_commandsPerString.Add(e.AddTerminalCommand.Command.ToLower(), (TERM_Command)num);
        term.m_command.m_commandHelpStrings.Add((TERM_Command)num, desc);
        term.m_command.m_commandEventMap.Add((TERM_Command)num, eventlist);

        for (int j = 0; j < eventlist.Count; j++)
        {
            WardenObjectiveEventData wardenObjectiveEventData = eventlist[j];
            if (wardenObjectiveEventData.ChainPuzzle != 0)
            {
                ChainedPuzzleDataBlock block = GameDataBlockBase<ChainedPuzzleDataBlock>.GetBlock(wardenObjectiveEventData.ChainPuzzle);
                if (block != null)
                {
                    ChainedPuzzleInstance chainPuzzle = ChainedPuzzleManager.CreatePuzzleInstance(block, term.SpawnNode.m_area, term.m_wardenObjectiveSecurityScanAlign.position, term.m_wardenObjectiveSecurityScanAlign, wardenObjectiveEventData.UseStaticBioscanPoints);
                    term.SetChainPuzzleForCommand((TERM_Command)num, j, chainPuzzle);
                }
            }
        }

        term.m_command.m_commandPostOutputMap.Add((TERM_Command)num, outputlist);
        term.TrySyncSetCommandShow((TERM_Command)num);
        term.TrySyncSetCommandRule((TERM_Command)num, e.AddTerminalCommand.SpecialCommandRule);
    }
}
