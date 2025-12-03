using CellMenu;
using HarmonyLib;
using Localization;
using SNetwork;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch(typeof(CM_PageExpeditionSuccess), nameof(CM_PageExpeditionSuccess.Update))]
internal static class Patch_SuccessPageExitButton
{
    [HarmonyPrefix]
    private static void Pre_FixButtonText(CM_PageExpeditionSuccess __instance)
    {
        __instance.m_btnLeaveExpedition.SetText(SNet.IsMaster ? Text.Get(913u) : Text.Get(914u)); // refresh the button text
    }
}