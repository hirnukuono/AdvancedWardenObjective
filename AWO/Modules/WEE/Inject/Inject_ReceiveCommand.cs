
/* using AWO.Modules.WEE;
using LevelGeneration;
using HarmonyLib;
using AWO.WEE.Events;
using System.Diagnostics;
using UnityEngine;
using Il2CppMono.Unity;

namespace AWO.Modules.WEE.Inject;

[HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReceiveCommand))]
internal class Inject_ReceiveCommand
{

    [HarmonyPrefix]
    static bool Prefix(LG_ComputerTerminalCommandInterpreter __instance, ref TERM_Command cmd, ref string inputLine, string param1, string param2)
    {
        if (EntryPoint.AWOCommandList == null) return true;
        bool flag = false;
        foreach (var awocmd in EntryPoint.AWOCommandList)
        {
            LG_Zone gaa;
            var zone = __instance.m_terminal.m_terminalItem.SpawnNode.m_zone;
            Builder.Current.m_currentFloor.TryGetZoneByLocalIndex(awocmd.data.DimensionIndex, awocmd.data.Layer, awocmd.data.LocalIndex, out gaa);
            if (gaa == zone) if (gaa.TerminalsSpawnedInZone[awocmd.data.AddTerminalCommand.TerminalIndex] == __instance.m_terminal)
                {
                    if (inputLine == awocmd.data.AddTerminalCommand.Command)
                    {
                        UnityEngine.Debug.Log($"AdvancedWardenObjective - stopped ReceiveCommand from saying {inputLine} is not a valid command");
                        cmd = TERM_Command.EmptyLine;
                        inputLine = "";
                        return true;
                    }
                }
        }

        if (cmd != TERM_Command.Commands) return true;
        UnityEngine.Debug.Log($"AdvancedWardenObjective - COMMANDS is mine mwahahaha");
        __instance.AddOutput("Available Commands:");
        foreach (var item in __instance.m_commandsPerEnum)
        {
            if (!__instance.m_terminal.CommandIsHidden(item.Key))
            {
                __instance.AddOutput(item.Value.ToUpper() + "<indent=20%>" + __instance.m_commandHelpStrings[item.Key] + "</indent>", spacing: false);
                __instance.AddOutput("");
            }
        }
        foreach (var awocmd in EntryPoint.AWOCommandList)
        {
            LG_Zone gaa;
            var zone = __instance.m_terminal.m_terminalItem.SpawnNode.m_zone;
            Builder.Current.m_currentFloor.TryGetZoneByLocalIndex(awocmd.data.DimensionIndex, awocmd.data.Layer, awocmd.data.LocalIndex, out gaa);
            if (gaa == zone) if (gaa.TerminalsSpawnedInZone[awocmd.data.AddTerminalCommand.TerminalIndex] == __instance.m_terminal)
                {
                    __instance.AddOutput(awocmd.data.AddTerminalCommand.Command.ToUpper() + "<indent=20%>" + awocmd.data.AddTerminalCommand.CommandDesc + "</indent>", spacing: false);
                    __instance.AddOutput("");
                }
        }
        __instance.AddOutput("");
        cmd = TERM_Command.EmptyLine;
        inputLine = "";
        return true;
    }
}
*/