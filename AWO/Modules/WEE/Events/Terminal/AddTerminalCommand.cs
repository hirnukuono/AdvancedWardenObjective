using AmorLib.Utils;
using AWO.Modules.TSL;
using ChainedPuzzles;
using GameData;
using GTFO.API.Extensions;
using LevelGeneration;
using Localization;

namespace AWO.Modules.WEE.Events;

internal sealed class AddTerminalCommand : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.AddTerminalCommand;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerCommon(WEE_EventData e)
    {
        foreach (var addcmd in e.AddTerminalCommand.Values)
        {
            if (!TryGetTerminalFromZone(e, addcmd.TerminalIndex, out var term)) 
                continue;

            TERM_Command c_num = (TERM_Command)(50 + addcmd.CommandNumber);
            if (term.m_command.m_commandsPerEnum.ContainsKey(c_num))
            {
                LogError($"A command with index {c_num} is already present on terminal!");
                return;
            }

            string helpString = SerialLookupManager.ParseTextFragments(addcmd.CommandDesc);
            if (addcmd.AutoIndentCommandDesc)
            {
                helpString = "<indent=20%>" + helpString + "</indent>";
            }
            var eventList = addcmd.CommandEvents.ToIl2Cpp();

            term.m_command.m_commandsPerEnum.Add(c_num, addcmd.Command.ToLower());
            term.m_command.m_commandsPerString.Add(addcmd.Command.ToLower(), c_num);
            term.m_command.m_commandHelpStrings.Add(c_num, new() { UntranslatedText = helpString, Id = 0u });
            term.m_command.m_commandEventMap.Add(c_num, eventList);

            ChainedPuzzleInstance? cmdChainPuzzle = null;
            for (int i = 0; i < eventList.Count; i++)
            {
                var eventData = eventList[i];
                if (eventData.ChainPuzzle != 0u)
                {
                    if (!DataBlockUtil.TryGetBlock<ChainedPuzzleDataBlock>(eventData.ChainPuzzle, out var block))
                    {
                        LogWarning($"Failed to find enabled ChainedPuzzleDataBlock {eventData.ChainPuzzle}!");
                        continue;
                    }                    
                    var spawnNode = term.SpawnNode ?? CourseNodeUtil.GetCourseNode(term.m_position, e.DimensionIndex);
                    cmdChainPuzzle = ChainedPuzzleManager.CreatePuzzleInstance(block, spawnNode.m_area, term.m_wardenObjectiveSecurityScanAlign.position, term.m_wardenObjectiveSecurityScanAlign, eventData.UseStaticBioscanPoints);
                    term.SetChainPuzzleForCommand(c_num, i, cmdChainPuzzle);
                }
                if (cmdChainPuzzle != null)
                {
                    cmdChainPuzzle.OnPuzzleSolved += (Action)(() => WOManager.CheckAndExecuteEventsOnTrigger(eventData, eWardenObjectiveEventTrigger.None, true));
                }
            }

            var postCmdOutputs = addcmd.PostCommandOutputs.ConvertAll(locale => locale.ToTerminalOutput());
            if (addcmd.ProgressWaitBeforeEvents)
            {
                var progressWait = new TerminalOutput()
                {
                    LineType = TerminalLineType.ProgressWait,
                    Output = new() { UntranslatedText = Text.Get(401434557), Id = 0u},
                    Time = 3f
                };
                postCmdOutputs.Insert(0, progressWait);
            }
            term.m_command.m_commandPostOutputMap.Add(c_num, postCmdOutputs.ToIl2Cpp());
            term.TrySyncSetCommandRule(c_num, addcmd.SpecialCommandRule);
        }
    }
}
