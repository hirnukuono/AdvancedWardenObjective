using CellMenu;
using GTFO.API;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch(typeof(MainMenuGuiLayer), nameof(MainMenuGuiLayer.AddPage))]
internal static class Patch_AddPage
{
    public static bool McBased = false;

    [HarmonyPrefix]
    [HarmonyWrapSafe]
    private static bool Pre_AddPage(MainMenuGuiLayer __instance, eCM_MenuPage pageEnum, string pageResourcePath, ref CM_PageBase __result)
    {
        if (GameStateManager.Current.m_currentStateName == eGameStateName.Startup || !pageResourcePath.Contains('/'))
        {
            return true;
        }

        if (pageEnum == eCM_MenuPage.CMP_EXPEDITION_SUCCESS && !McBased)
        {
            Logger.Info("Credits to McBreezy for CustomSuccessScreens");
            McBased = true;
        }

        try
        {
            ((Il2CppArrayBase<CM_PageBase>)(object)__instance.m_pages)[(int)pageEnum] = GOUtil.SpawnChildAndGetComp<CM_PageBase>(AssetAPI.GetLoadedAsset<GameObject>(pageResourcePath), __instance.GuiLayerBase.transform);
            ((Il2CppArrayBase<CM_PageBase>)(object)__instance.m_pages)[(int)pageEnum].Setup(__instance);
            __result = ((Il2CppArrayBase<CM_PageBase>)(object)__instance.m_pages)[(int)pageEnum];
            __result.OnResolutionChange(__instance.m_currentScaledRes);
        }
        catch
        {
            Logger.Error($"CustomSuccessScreen {pageResourcePath} not found!!!");
            return true;
        }

        return false;
    }
}