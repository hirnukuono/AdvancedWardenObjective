using AWO.Jsons;
using GameData;
using HarmonyLib;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch(typeof(MainMenuGuiLayer), nameof(MainMenuGuiLayer.OnExpeditionUpdated))]
internal static class Patch_OnExpeditionUpdated
{
    private static void Prefix(ExpeditionInTierData expeditionInTierData, out string? __state)
    {
        if (WinScreen.VanillaPaths.Contains(expeditionInTierData.SpecialOverrideData.CustomSuccessScreen))
        {
            __state = null;
            return;
        }

        __state = new(expeditionInTierData.SpecialOverrideData.CustomSuccessScreen);
        expeditionInTierData.SpecialOverrideData.CustomSuccessScreen = null;
    }

    private static void Postfix(MainMenuGuiLayer __instance, string? __state)
    {
        if (__state?.Contains('/') == true)
        {
            __instance.PageCustomExpeditionSuccess = __instance.AddPage(eCM_MenuPage.CMP_EXPEDITION_SUCCESS, __state);
            Logger.Debug($"Successfully loaded modded CustomSuccessScreen");
        }
    }
}