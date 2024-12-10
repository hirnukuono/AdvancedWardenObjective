using HarmonyLib;

namespace AWO.Modules.TerminalSerialLookup;

[HarmonyPatch]
internal static class Patch_IncomingFragments
{
    [HarmonyPatch(typeof(WOManager), nameof(WOManager.ReplaceFragmentsInString))]
    private static void Postfix(ref string __result)
    {
        if (HasBracketPair(__result))
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
