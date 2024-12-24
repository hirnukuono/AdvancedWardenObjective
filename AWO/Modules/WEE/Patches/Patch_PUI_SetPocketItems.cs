using HarmonyLib;
using static AWO.Modules.WEE.Events.SetPocketItemEvent;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch]
internal static class Patch_PUI_SetPocketOtems
{
    [HarmonyPatch(typeof(PUI_GameObjectives), nameof(PUI_GameObjectives.SetItems))]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    private static void Pre_SetItems(ref string txt)
    {
        if (HasEmptyPockets) return;

        txt = string.Join("\n", new[] { TopItems, txt, BottomItems }.Where(section => !string.IsNullOrWhiteSpace(section)));
    }
}
