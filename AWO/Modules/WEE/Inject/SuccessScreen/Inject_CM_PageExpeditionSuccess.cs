using CellMenu;
using HarmonyLib;
using Localization;
using SNetwork;

namespace AWO.WEE.Inject;

[HarmonyPatch(typeof(CM_PageExpeditionSuccess), "Update")]
internal static class Inject_CM_PageExpeditionSuccess
{
    public static void Prefix(CM_PageExpeditionSuccess __instance)
    {
        if (SNet.IsMaster)
            __instance.m_btnLeaveExpedition.SetText(Text.Get(913u));
        else
            __instance.m_btnLeaveExpedition.SetText(Text.Get(914u));
    }
}