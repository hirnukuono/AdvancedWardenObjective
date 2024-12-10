using ChainedPuzzles;
using GameData;
using GTFO.API.Extensions;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class AddTerminalCommand : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.AddTerminalCommand;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) return;

        var term = zone.TerminalsSpawnedInZone[e.AddTerminalCommand.TerminalIndex];
        TERM_Command c_num = (TERM_Command)(50 + e.AddTerminalCommand.CommandNumber);
        if (term.m_command.m_commandsPerEnum.ContainsKey(c_num))
        {
            LogError($"A command with index {c_num} is already present on terminal!");
            return;
        }

        Il2CppSystem.Collections.Generic.List<WardenObjectiveEventData> eventList = e.AddTerminalCommand.CommandEvents.ToIl2Cpp();

        term.m_command.m_commandsPerEnum.Add(c_num, e.AddTerminalCommand.Command.ToLower());
        term.m_command.m_commandsPerString.Add(e.AddTerminalCommand.Command.ToLower(), c_num);
        term.m_command.m_commandHelpStrings.Add(c_num, new() { UntranslatedText = e.AddTerminalCommand.CommandDesc });
        term.m_command.m_commandEventMap.Add(c_num, eventList);

        for (int i = 0; i < eventList.Count; i++)
        {
            WardenObjectiveEventData eventData = eventList[i];
            if (eventData.ChainPuzzle != 0)
            {
                var block = GameDataBlockBase<ChainedPuzzleDataBlock>.GetBlock(eventData.ChainPuzzle);
                if (block == null || !block.internalEnabled)
                {
                    LogError("Failed to find enabled ChainedPuzzleDataBlock!");
                    return;
                }

                var chainPuzzle = ChainedPuzzleManager.CreatePuzzleInstance(block, term.SpawnNode.m_area, term.m_wardenObjectiveSecurityScanAlign.position, term.m_wardenObjectiveSecurityScanAlign, eventData.UseStaticBioscanPoints);
                term.SetChainPuzzleForCommand(c_num, i, chainPuzzle);
            }
        }

        term.m_command.m_commandPostOutputMap.Add(c_num, e.AddTerminalCommand.PostCommandOutputs.ToIl2Cpp());
        term.TrySyncSetCommandShow(c_num);
        term.TrySyncSetCommandRule(c_num, e.AddTerminalCommand.SpecialCommandRule);
    }
}
