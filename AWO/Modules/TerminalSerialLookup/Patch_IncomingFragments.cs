using BepInEx;
using HarmonyLib;

namespace AWO.Modules.TerminalSerialLookup;

[HarmonyPatch]
internal static class Patch_IncomingFragments
{
    [HarmonyPatch(typeof(WOManager), nameof(WOManager.ReplaceFragmentsInString))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_ReplaceFragments(ref string __result)
    {
        if (HasBracketPair(__result))
        {
            __result = SerialLookupManager.ParseTextFragments(__result);
        }
    }

    private static bool HasBracketPair(string input)
    {
        if (input.IsNullOrWhiteSpace()) return false;

        int open = input.IndexOf('[');
        int close = input.IndexOf(']', open + 1);
        return open >= 0 && open < close;
    }
}
