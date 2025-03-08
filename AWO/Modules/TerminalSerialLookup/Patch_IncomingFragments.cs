using HarmonyLib;
using LevelGeneration;

namespace AWO.Modules.TerminalSerialLookup;

[HarmonyPatch]
internal static class Patch_IncomingFragments
{
    [HarmonyPatch(typeof(WOManager), nameof(WOManager.ReplaceFragmentsInString))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_ReplaceFragments(ref string __result)
    {
        __result = SerialLookupManager.ParseTextFragments(__result);
    }

    [HarmonyPatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.OnDoorState))]
    [HarmonyPostfix]
    [HarmonyAfter]
    [HarmonyWrapSafe]
    private static void InteractText_OnDoorState(LG_SecurityDoor_Locks __instance, pDoorState state)
    {
        if (state.status == eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm)
        {
            __instance.m_intOpenDoor.InteractionMessage = SerialLookupManager.ParseTextFragments(__instance.m_intOpenDoor.InteractionMessage);
        }
    }
}
