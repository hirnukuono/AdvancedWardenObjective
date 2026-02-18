using AWO.Modules.WEE.Events;
using HarmonyLib;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch(typeof(PUI_WardenIntel), nameof(PUI_WardenIntel.ShowSubObjectiveMessage))]
internal static class Patch_PUI_WardenIntel
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    private static bool Pre_ShowSubObjectiveMessage(ref string lastSubObj)
    {
        if (lastSubObj == "[GTFO.AWO]")
        {
            lastSubObj = string.Empty;
            return true;
        }
        return ClearWardenIntelQueueEvent.AllowWardenIntel;
    }
}
