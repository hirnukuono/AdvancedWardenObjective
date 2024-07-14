using AWO.Jsons;
using GameData;
using HarmonyLib;

namespace AWO.WEE.Inject;

[HarmonyPatch(typeof(MainMenuGuiLayer), "OnExpeditionUpdated")]
internal static class Inject_MainMenuGuiLayer
{
    private static Dictionary<string, string> CachedCustomExpeditions = new();

    private static string BackupOverride = string.Empty;

    public static void Prefix(MainMenuGuiLayer __instance, pActiveExpedition activeExpedition, ExpeditionInTierData expeditionInTierData)
    {
        if (CachedCustomExpeditions.TryGetValue(expeditionInTierData.Descriptive.PublicName, out string value))
            BackupOverride = value;
        else
            BackupOverride = expeditionInTierData.SpecialOverrideData.CustomSuccessScreen;

        if (!WinScreen.v_WinScreen.Contains(expeditionInTierData.SpecialOverrideData.CustomSuccessScreen))
        {
            if (!CachedCustomExpeditions.ContainsKey(expeditionInTierData.Descriptive.PublicName))
            {
                CachedCustomExpeditions.Add(expeditionInTierData.Descriptive.PublicName, expeditionInTierData.SpecialOverrideData.CustomSuccessScreen);
            }
            expeditionInTierData.SpecialOverrideData.CustomSuccessScreen = null;
        }
    }

    public static void Postfix(MainMenuGuiLayer __instance, pActiveExpedition activeExpedition, ExpeditionInTierData expeditionInTierData)
    {
        if (BackupOverride != null && BackupOverride.Contains('/'))
        {
            __instance.PageCustomExpeditionSuccess = __instance.AddPage(eCM_MenuPage.CMP_EXPEDITION_SUCCESS, BackupOverride);
            Logger.Debug($"Loaded modded CustomSuccessScreen");
        }
    }
}