using AWO.Modules.WEE.Events;
using HarmonyLib;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch(typeof(PUI_GameObjectives), nameof(PUI_GameObjectives.SetItems))]
internal static class Patch_PUI_SetPocketItems
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    private static void Pre_SetItems(ref string txt)
    {
        if (SetPocketItemEvent.HasEmptyPockets) 
            return;

        txt = string.Join("\n", new[] { SetPocketItemEvent.TopItems, txt, SetPocketItemEvent.BottomItems }.Where(section => !string.IsNullOrWhiteSpace(section)));
    }
}
