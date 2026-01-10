using AmorLib.Utils;
using HarmonyLib;
using LevelGeneration;
using static AWO.Modules.WEE.Events.StartPortalEvent;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch(typeof(LG_DimensionPortal), nameof(LG_DimensionPortal.Setup))]
internal static class Patch_LG_DimensionPortal
{
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_Setup(LG_DimensionPortal __instance)
    {
        Portals.Add(__instance.SpawnNode.m_zone.ToStruct(), __instance);
    }
}
