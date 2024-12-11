using AWO.Modules.WEE.Replicators;
using HarmonyLib;
using LevelGeneration;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch]
internal static class Patch_ZoneLightJob
{
    [HarmonyPatch(typeof(LG_BuildZoneLightsJob), nameof(LG_BuildZoneLightsJob.Build))]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    private static void Pre_ZoneBuild(LG_BuildZoneLightsJob __instance, out ZoneLightReplicator? __state)
    {
        var zone = __instance.m_zone;
        if (zone == null)
        {
            __state = null;
            return;
        }

        __state = zone.gameObject.AddOrGetComponent<ZoneLightReplicator>();
        if (!__state.IsSetup)
        {
            __state.Setup(zone);
        }
    }

    [HarmonyPatch(typeof(LG_BuildZoneLightsJob), nameof(LG_BuildZoneLightsJob.Build))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_ZoneBuild(bool __result, ZoneLightReplicator? __state)
    {
        if (!__result) return;

        __state?.Setup_UpdateLightSetting();
    }
}
