using HarmonyLib;
using LevelGeneration;

namespace AWO.Modules.WOE;

public delegate void SetupObjectiveDel(LG_LayerType layer, int chainIndex);

[Obsolete]
internal static class WOEvents
{
    public static event SetupObjectiveDel? OnSetup;

    internal static void Invoke_OnSetup(LG_LayerType layer, int chainIndex) => OnSetup?.Invoke(layer, chainIndex);
}

/*
[Obsolete]
[HarmonyPatch]
internal static class Patch_WOManager
{
    [HarmonyPatch(typeof(WOManager), nameof(WOManager.SetupWardenObjectiveLayer))]
    [HarmonyPostfix]
    private static void Post_WOSetup(LG_LayerType layer, int chainIndex)
    {
        WOEvents.Invoke_OnSetup(layer, chainIndex);
    }
}
*/
