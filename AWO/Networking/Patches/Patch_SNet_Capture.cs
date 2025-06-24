using HarmonyLib;
using SNetwork;

namespace AWO.Networking.Patches;

[HarmonyPatch(typeof(SNet_Capture))]
internal static class Patch_SNet_Capture
{
    public static event Action<eBufferType>? OnBufferCapture;
    public static event Action<eBufferType>? OnBufferRecalled;

    [HarmonyPatch(nameof(SNet_Capture.TriggerCapture))]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    static void Pre_TriggerCapture(SNet_Capture __instance)
    {
        var type = __instance.PrimedBufferType;
        OnBufferCapture?.Invoke(type);
    }

    [HarmonyPatch(nameof(SNet_Capture.RecallBuffer))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    static void Post_RecallBuffer(SNet_Capture __instance, eBufferType bufferType)
    {
        if (__instance.IsRecalling) return;

        OnBufferRecalled?.Invoke(bufferType);
    }
}