using AWO.Jsons;
using CellMenu;
using GameData;
using HarmonyLib;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch]
internal static class Patch_OnExpeditionUpdated
{
    [HarmonyPatch(typeof(MainMenuGuiLayer), nameof(MainMenuGuiLayer.OnExpeditionUpdated))]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    private static void Pre_ExpeditionUpdated(ExpeditionInTierData expeditionInTierData, out string? __state)
    {
        if (WinScreen.VanillaPaths.Contains(expeditionInTierData.SpecialOverrideData.CustomSuccessScreen))
        {
            __state = null;
            return;
        }

        __state = new(expeditionInTierData.SpecialOverrideData.CustomSuccessScreen);
        expeditionInTierData.SpecialOverrideData.CustomSuccessScreen = null;
    }

    [HarmonyPatch(typeof(MainMenuGuiLayer), nameof(MainMenuGuiLayer.OnExpeditionUpdated))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_ExpeditionUpdated(MainMenuGuiLayer __instance, string? __state)
    {
        if (!string.IsNullOrEmpty(__state) && __state.Contains('/'))
        {
            __instance.PageCustomExpeditionSuccess = __instance.AddPage(eCM_MenuPage.CMP_EXPEDITION_SUCCESS, __state);
            Logger.Debug($"Successfully loaded modded CustomSuccessScreen");
        }
    }

    [HarmonyPatch(typeof(GameStateManager), nameof(GameStateManager.DoChangeState))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_DoChangeState(GameStateManager __instance, eGameStateName nextState)
    {
        if (nextState == eGameStateName.Lobby)
        {
            foreach (var p in RundownManager.FindObjectsOfType<CM_PageExpeditionSuccess>())
            {
                p.SetPageActive(false);
            }
        }
    }
}