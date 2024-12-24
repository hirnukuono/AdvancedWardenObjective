using GameData;
using HarmonyLib;
using LevelGeneration;
using System.Collections;
using UnityEngine;
using static AWO.Modules.WEE.Events.SetTerminalLog;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch]
internal static class Patch_CmdInterpreterReadLog
{
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReadLog))]
    [HarmonyPostfix]
    private static void Post_ReadLog(LG_ComputerTerminalCommandInterpreter __instance, string param1)
    {
        if (LogEventQueue.TryGetValue((__instance.m_terminal.SyncID, param1), out var eData))
        {
            CoroutineManager.StartCoroutine(DoEvents(eData).WrapToIl2Cpp());
        }
    }

    private static IEnumerator DoEvents(Queue<WardenObjectiveEventData> eData)
    {
        yield return new WaitForSeconds(3.0f); // wait for line output done
        while (eData.Count > 0)
        {
            WOManager.CheckAndExecuteEventsOnTrigger(eData.Dequeue(), eWardenObjectiveEventTrigger.None);
        }
    }
}