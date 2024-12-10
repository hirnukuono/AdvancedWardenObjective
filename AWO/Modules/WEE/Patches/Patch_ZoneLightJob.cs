using AWO.Modules.WEE.Replicators;
using HarmonyLib;
using LevelGeneration;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch(typeof(LG_BuildZoneLightsJob), nameof(LG_BuildZoneLightsJob.Build))]
internal static class Patch_ZoneLightJob
{
    private static void Prefix(LG_BuildZoneLightsJob __instance, out ZoneLightReplicator? __state)
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

    private static void Postfix(bool __result, ZoneLightReplicator? __state)
    {
        if (!__result) return;

        __state?.Setup_UpdateLightSetting();
    }
}
