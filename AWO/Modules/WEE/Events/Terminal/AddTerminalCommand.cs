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
        var addcmd = e.AddTerminalCommand;
        if (!TryGetTerminalFromZone(e, addcmd.TerminalIndex, out var term)) return;
        TERM_Command c_num = (TERM_Command)(50 + addcmd.CommandNumber);
        if (term.m_command.m_commandsPerEnum.ContainsKey(c_num))
        {
            LogError($"A command with index {c_num} is already present on terminal!");
            return;
        }

        Il2CppSystem.Collections.Generic.List<WardenObjectiveEventData> eventList = addcmd.CommandEvents.ToIl2Cpp();

        term.m_command.m_commandsPerEnum.Add(c_num, addcmd.Command.ToLower());
        term.m_command.m_commandsPerString.Add(addcmd.Command.ToLower(), c_num);
        term.m_command.m_commandHelpStrings.Add(c_num, new() { UntranslatedText = addcmd.AutoIndentCommandDesc ? $"<indent=20%>{addcmd.CommandDesc}</indent>" : addcmd.CommandDesc });
        term.m_command.m_commandEventMap.Add(c_num, eventList);

        for (int i = 0; i < eventList.Count; i++)
        {
            WardenObjectiveEventData eventData = eventList[i];
            if (eventData.ChainPuzzle != 0u)
            {
                var block = GameDataBlockBase<ChainedPuzzleDataBlock>.GetBlock(eventData.ChainPuzzle);
                if (block == null || !block.internalEnabled)
                {
                    LogWarning("Failed to find enabled ChainedPuzzleDataBlock!");
                    continue;
                }

                var chainPuzzle = ChainedPuzzleManager.CreatePuzzleInstance(block, term.SpawnNode.m_area, term.m_wardenObjectiveSecurityScanAlign.position, term.m_wardenObjectiveSecurityScanAlign, eventData.UseStaticBioscanPoints);
                term.SetChainPuzzleForCommand(c_num, i, chainPuzzle);
            }
        }

        term.m_command.m_commandPostOutputMap.Add(c_num, addcmd.PostCommandOutputs.ToIl2Cpp());
        term.TrySyncSetCommandShow(c_num);
        term.TrySyncSetCommandRule(c_num, addcmd.SpecialCommandRule);
    }
}
