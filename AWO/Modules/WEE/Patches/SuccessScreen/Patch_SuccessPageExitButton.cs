using CellMenu;
using HarmonyLib;
using Localization;
using SNetwork;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch]
internal static class Patch_SuccessPageExitButton
{
    [HarmonyPatch(typeof(CM_PageExpeditionSuccess), nameof(CM_PageExpeditionSuccess.Update))]
    [HarmonyPrefix]
    private static void Pre_FixButtonText(CM_PageExpeditionSuccess __instance)
    {
        __instance.m_btnLeaveExpedition.SetText(SNet.IsMaster ? Text.Get(913u) : Text.Get(914u)); // refresh the button text
    }
}