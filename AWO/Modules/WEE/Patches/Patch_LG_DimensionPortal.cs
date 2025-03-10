﻿using HarmonyLib;
using LevelGeneration;
using static AWO.Modules.WEE.Events.StartPortalEvent;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch]
internal static class Patch_LG_DimensionPortal
{
    [HarmonyPatch(typeof(LG_DimensionPortal), nameof(LG_DimensionPortal.Setup))]
    [HarmonyPostfix]
    private static void Post_Setup(LG_DimensionPortal __instance)
    {
        Portals.Add(new(__instance.SpawnNode.m_dimension.DimensionIndex, __instance.SpawnNode.LayerType, __instance.SpawnNode.m_zone.LocalIndex), __instance);
    }
}
