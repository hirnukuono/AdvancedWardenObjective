using BepInEx.Unity.IL2CPP.Utils;
using GameData;
using HarmonyLib;
using LevelGeneration;
using System.Collections;
using UnityEngine;
using static AWO.Modules.WEE.Events.SetTerminalLog;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch]
internal static class Patch_TermCmdInterpreter
{
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReceiveCommand))]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    private static void Pre_ReceiveCommand(LG_ComputerTerminalCommandInterpreter __instance, TERM_Command cmd, string inputLine, string param1, string param2)
    {
        if (cmd == TERM_Command.ReadLog)
        {
            if (LogEventQueue.TryGetValue((__instance.m_terminal.SyncID, param1.ToUpper()), out var eData))
            {
                __instance.m_terminal.StartCoroutine(DoEvents(eData));
            }
        }
    }

    private static IEnumerator DoEvents(Queue<WardenObjectiveEventData> eData)
    {
        yield return new WaitForSeconds(3f); // wait for line output done, i.e. log viewable
        while (eData.Count > 0)
        {
            WOManager.CheckAndExecuteEventsOnTrigger(eData.Dequeue(), eWardenObjectiveEventTrigger.None, true);
        }
    }
}