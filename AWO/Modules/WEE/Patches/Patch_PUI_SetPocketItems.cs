using HarmonyLib;
using static AWO.Modules.WEE.Events.SetPocketItemEvent;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch(typeof(PUI_GameObjectives), nameof(PUI_GameObjectives.SetItems))]
internal static class Patch_PUI_SetPocketItems
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    private static void Pre_SetItems(ref string txt)
    {
        if (HasEmptyPockets) return;

        txt = string.Join("\n", new[] { TopItems, txt, BottomItems }.Where(section => !string.IsNullOrWhiteSpace(section)));
    }
}
