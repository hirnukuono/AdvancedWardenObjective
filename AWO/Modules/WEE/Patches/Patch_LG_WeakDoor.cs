using AmorLib.Utils.Extensions;
using AWO.Modules.WEE.Replicators;
using HarmonyLib;
using LevelGeneration;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch(typeof(LG_WeakDoor), nameof(LG_WeakDoor.Setup))]
internal static class Patch_LG_WeakDoor
{
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_Setup(LG_WeakDoor __instance)
    {
        var zone = __instance.Gate.CoursePortal.m_nodeA.m_zone;
        zone.gameObject.AddOrGetComponent<WeakDoorInfoReplicator>().Setup(__instance);
    }
}
