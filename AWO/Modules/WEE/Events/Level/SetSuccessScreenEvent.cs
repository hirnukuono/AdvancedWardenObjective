using BepInEx.Logging;
using CellMenu;
using GTFO.API;
using Localization;
using System.Collections;
using UnityEngine;
using ScreenType = AWO.Modules.WEE.WEE_SetSuccessScreen.ScreenType;

namespace AWO.Modules.WEE.Events;

internal sealed class SetSuccessScreenEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetSuccessScreen;

    private static CM_PageExpeditionSuccess? s_storedPage = null;
    private static string s_storedSuccessText = string.Empty;
    private static bool s_changedOrigText = false;
    private static bool s_shouldResetMusic = false;

    protected override void TriggerCommon(WEE_EventData e)
    {
        e.SuccessScreen ??= new();
        switch (e.SuccessScreen.Type)
        {
            case ScreenType.SetSuccessScreen:
                SetScreen(e);
                break;

            case ScreenType.FlashFakeScreen:
                CoroutineManager.StartCoroutine(FakeScreen(e).WrapToIl2Cpp());
                break;
        }
    }
    
    private static void SetScreen(WEE_EventData e)
    {
        string pageResourcePath = e.SuccessScreen!.CustomSuccessScreen;
        if (pageResourcePath != string.Empty)
        {
            try
            {
                RestoreSuccessText();
                SetSuccessPage(pageResourcePath);
            }
            catch
            {
                Logger.Error("SetSuccessScreen", $"CustomSuccessScreen asset {pageResourcePath} not found!");
            }
        }

        SetSuccessText(e.SpecialText);
        SetSuccessMusic(e.SuccessScreen.OverrideMusic);
    }

    static IEnumerator FakeScreen(WEE_EventData e)
    {
        Logger.Verbose(LogLevel.Debug, "Enabling fake end screen... Disabled map and menu toggle");
        SetSuccessText(e.SpecialText);
        FocusStateManager.EnterMenu(e.SuccessScreen!.FakeEndScreen, force: true);
        FocusStateManager.MapToggleAllowed = false;
        FocusStateManager.MenuToggleAllowed = false;

        yield return new WaitForSeconds(e.Duration);

        Logger.Verbose(LogLevel.Debug, "Disabling fake end screen... Enabled map and menu toggle");
        RestoreSuccessText();
        FocusStateManager.ExitMenu();
        FocusStateManager.ChangeState(eFocusState.FPS, force: true);
        FocusStateManager.MapToggleAllowed = true;
        FocusStateManager.MenuToggleAllowed = true;
    }

    private static void SetSuccessPage(string pageResourcePath)
    {
        if (s_storedPage == null)
        {
            s_storedPage = MainMenuGuiLayer.Current.PageExpeditionSuccess;
            LevelAPI.OnBuildStart += RestoreSuccessPage; // Any event that fires after the player leaves the success screen
        }
        else
            GameObject.Destroy(MainMenuGuiLayer.Current.PageExpeditionSuccess.gameObject);

        MainMenuGuiLayer.Current.PageExpeditionSuccess = MainMenuGuiLayer.Current.AddPage(eCM_MenuPage.CMP_EXPEDITION_SUCCESS, pageResourcePath).Cast<CM_PageExpeditionSuccess>();
        Logger.Verbose(LogLevel.Debug, $"CustomSuccessScreen should now be changed to {pageResourcePath}");
    }

    private static void SetSuccessText(string text)
    {
        if (text == string.Empty)
            return;

        var header = MainMenuGuiLayer.Current.PageExpeditionSuccess.m_header;
        var localizer = header.GetComponent<TMP_Localizer>();
        if (s_storedSuccessText == string.Empty)
        {
            s_storedSuccessText = localizer != null ? Text.Get(localizer.m_blockId) : header.text;
            s_changedOrigText = s_storedPage == null;
            if (s_changedOrigText)
                LevelAPI.OnBuildStart += RestoreSuccessText; // Any event that fires after the player leaves the success screen
        }

        header.SetText(text);
        if (localizer != null)
            GameObject.Destroy(localizer);

        Logger.Verbose(LogLevel.Debug, $"Set success screen text to {text}.");
    }
    
    private static void SetSuccessMusic(uint music)
    {
        if (!s_shouldResetMusic)
        {
            s_shouldResetMusic = true;
            LevelAPI.OnBuildStart += RestoreSuccessMusic; // Any event that fires after the player leaves the success screen
        }
        
        MainMenuGuiLayer.Current.PageExpeditionSuccess.m_overrideSuccessMusic = music;
        Logger.Verbose(LogLevel.Debug, $"Set success screen music to sound id {music}.");
    }

    private static void RestoreSuccessPage()
    {
        if (s_storedPage != null)
        {
            GameObject.Destroy(MainMenuGuiLayer.Current.PageExpeditionSuccess.gameObject);
            MainMenuGuiLayer.Current.PageExpeditionSuccess = s_storedPage;
            MainMenuGuiLayer.Current.m_pages[(int)eCM_MenuPage.CMP_EXPEDITION_SUCCESS] = s_storedPage;
            s_storedPage = null;
            LevelAPI.OnBuildStart -= RestoreSuccessPage;
        }
    }

    private static void RestoreSuccessText()
    {
        if (s_storedSuccessText != string.Empty)
        {
            MainMenuGuiLayer.Current.PageExpeditionSuccess.m_header.SetText(s_storedSuccessText);
            s_storedSuccessText = string.Empty;
            if (s_changedOrigText)
                LevelAPI.OnBuildStart -= RestoreSuccessText;
            s_changedOrigText = false;
        }
    }
    
    private static void RestoreSuccessMusic()
    {
        if (s_shouldResetMusic)
        {
            MainMenuGuiLayer.Current.PageExpeditionSuccess.m_overrideSuccessMusic = 0u; // 0 indicates the default music should be played
            s_shouldResetMusic = false;
            LevelAPI.OnBuildStart -= RestoreSuccessMusic;
        }
    }
}