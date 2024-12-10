using HarmonyLib;
using LevelGeneration;

namespace AWO.Sessions.Patches;

[HarmonyPatch]
internal static class Patch_InteractionOnBlackout
{
    [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.OnProximityEnter))]
    [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.OnProximityExit))]
    [HarmonyPatch(typeof(LG_DoorButton), nameof(LG_DoorButton.OnWeakLockUnlocked))]
    private static bool Prefix()
    {
        return !BlackoutState.BlackoutEnabled;
    }
}
