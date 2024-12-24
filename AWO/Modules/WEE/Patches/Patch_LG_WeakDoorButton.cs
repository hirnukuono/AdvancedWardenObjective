﻿using HarmonyLib;
using LevelGeneration;
using static AWO.Modules.WEE.Events.DoInteractWeakDoorsEvent;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch]
internal static class Patch_LG_WeakDoorButton
{
    [HarmonyPatch(typeof(LG_WeakDoor), nameof(LG_WeakDoor.Setup))]
    [HarmonyPostfix]
    private static void Post_Setup(LG_WeakDoor __instance)
    {
        WeakDoors.GetOrAddNew(__instance.Gate.CoursePortal.m_nodeA.m_zone.ID).Add(__instance);
    }
}
