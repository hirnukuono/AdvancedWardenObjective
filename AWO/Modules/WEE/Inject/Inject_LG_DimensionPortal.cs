using HarmonyLib;
using LevelGeneration;

namespace AWO.WEE.Inject;

[HarmonyPatch(typeof(LG_DimensionPortal), "Setup")]
internal static class Inject_LG_DimensionPortal
{
    public static Action<LG_DimensionPortal>? OnPortalSetup;

    public static void Postfix(LG_DimensionPortal __instance)
    {
        EntryPoint.Portals.Add(new GlobalZoneIndex(__instance.SpawnNode.m_zone.DimensionIndex, __instance.SpawnNode.LayerType, __instance.SpawnNode.m_zone.LocalIndex), __instance);
        OnPortalSetup?.Invoke(__instance);
    }
}
