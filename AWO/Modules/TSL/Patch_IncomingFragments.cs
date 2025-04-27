using HarmonyLib;
using LevelGeneration;
using static AWO.Modules.TSL.SerialLookupManager;

namespace AWO.Modules.TSL;

[HarmonyPatch]
internal static class Patch_IncomingFragments
{
    [HarmonyPatch(typeof(WOManager), nameof(WOManager.ReplaceFragmentsInString))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_ReplaceFragments(ref string __result)
    {
        __result = ParseTextFragments(__result);
    }

    [HarmonyPatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.OnDoorState))]
    [HarmonyPostfix]
    [HarmonyAfter]
    [HarmonyWrapSafe]
    private static void InteractText_OnDoorState(LG_SecurityDoor_Locks __instance, pDoorState state)
    {
        if (state.status == eDoorStatus.Closed_LockedWithChainedPuzzle || state.status == eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm)
        {
            __instance.m_intOpenDoor.InteractionMessage = ParseTextFragments(__instance.m_intOpenDoor.InteractionMessage);
        }
    }
}
