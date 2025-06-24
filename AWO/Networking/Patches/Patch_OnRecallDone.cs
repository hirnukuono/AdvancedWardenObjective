using HarmonyLib;
using SNetwork;

namespace AWO.Networking.Patches;

[HarmonyPatch(typeof(SNet_Capture))]
internal static class Patch_OnRecallDone
{
    public static event Action? OnRecallDone;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.OnRecallDone))]

    private static void Post_OnRecallDone()
    {
        OnRecallDone?.Invoke();
    }
}