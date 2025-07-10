using HarmonyLib;
using LevelGeneration;

namespace AWO.Modules.TSL;

[HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.GenericObjectiveSetup))]
internal static class Patch_ReactorTerminal
{
    [HarmonyPostfix]    
    [HarmonyWrapSafe]
    private static void Post_ReactorTerminalSetup(LG_WardenObjective_Reactor __instance)
    {
        if (__instance.SpawnNode.m_zone.TerminalsSpawnedInZone != null && __instance.m_terminal != null)
        {
            int dimension = (int)__instance.SpawnNode.m_dimension.DimensionIndex;
            int layer = (int)__instance.SpawnNode.LayerType;
            int zone = (int)__instance.SpawnNode.m_zone.LocalIndex;

            SerialLookupManager.ReactorTerminals.Add((dimension, layer, zone), __instance.m_terminal);
        }
    }
}
