using ChainedPuzzles;
using LevelGeneration;
using System.Collections;
using UnityEngine;

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
                var events = term.m_command.m_commandEventMap[command];
                term.m_command.m_commandEventMap.Remove(command);
                term.m_command.m_commandPostOutputMap.Remove(command);
                for (int i = 0; i < events.Count; i++)
                {
                    if (!term.TryGetChainPuzzleForCommand(command, i, out var puzzle) || puzzle == null) continue;
                    // If a puzzle is in use, command is not done; just let it leak, too much effort to clean up later
                    if (puzzle.IsActive)
                        break;
                    CoroutineManager.StartCoroutine(DestroyScanDelayed(puzzle).WrapToIl2Cpp());
                }
            }
            else if (IsMaster)
            {
                var state = term.m_stateReplicator.State;
                state.TryHideCommand(command);
                term.m_stateReplicator.State = state;
            }
        }
    }

    private static IEnumerator DestroyScanDelayed(ChainedPuzzleInstance scan)
    {
        yield return new WaitForSeconds(1f);
        foreach (var puzzle in scan.m_chainedPuzzleCores)
            if (puzzle != null)
                GameObject.Destroy(puzzle.Cast<MonoBehaviour>().gameObject);
        GameObject.Destroy(scan.gameObject);
    }
}
