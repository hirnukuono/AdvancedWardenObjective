using CellMenu;
using HarmonyLib;
using Localization;
using SNetwork;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch]
internal static class Patch_CM_PageExpeditionSuccess
{
    [HarmonyPatch(typeof(CM_PageExpeditionSuccess), nameof(CM_PageExpeditionSuccess.Update))]
    private static void Prefix(CM_PageExpeditionSuccess __instance)
    {
        if (SNet.IsMaster)
            __instance.m_btnLeaveExpedition.SetText(Text.Get(913u));
        else
            __instance.m_btnLeaveExpedition.SetText(Text.Get(914u));
    }
}