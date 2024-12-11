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
        if (!__result.IsNullOrWhiteSpace() && HasBracketPair(__result))
        {
            __result = SerialLookupManager.ParseTextFragments(__result);
        }
    }

    private static bool HasBracketPair(string input)
    {
        int open = input.IndexOf('[');
        int close = input.IndexOf(']', open + 1);
        return open >= 0 && open < close;
    }
}
