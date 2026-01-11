using BepInEx.Logging;
using CellMenu;
using GTFO.API;
using System.Collections;
using UnityEngine;
using ScreenType = AWO.Modules.WEE.WEE_SetSuccessScreen.ScreenType;

namespace AWO.Modules.WEE.Events;
internal sealed class SetSuccessScreenEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetSuccessScreen;

    private static string s_storedSuccessText = string.Empty;
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
                MainMenuGuiLayer.Current.PageExpeditionSuccess = MainMenuGuiLayer.Current.AddPage(eCM_MenuPage.CMP_EXPEDITION_SUCCESS, pageResourcePath).Cast<CM_PageExpeditionSuccess>();
                Logger.Verbose(LogLevel.Debug, $"CustomSuccessScreen should now be changed to {pageResourcePath}");
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

    private static void SetSuccessText(string text)
    {
        if (text == string.Empty)
            return;

        if (s_storedSuccessText == string.Empty)
        {
            s_storedSuccessText = MainMenuGuiLayer.Current.PageExpeditionSuccess.m_header.text;
            LevelAPI.OnBuildStart += RestoreSuccessText; // Any event that fires after the player leaves the success screen
        }

        MainMenuGuiLayer.Current.PageExpeditionSuccess.m_header.SetText(text);
        Logger.Verbose(LogLevel.Debug, $"Set success screen text to {text}.");
    }
    
    private static void SetSuccessMusic(uint music)
    {
        if (!s_shouldResetMusic)
        {
            LevelAPI.OnBuildStart += RestoreSuccessMusic; // Any event that fires after the player leaves the success screen
        }
        
        MainMenuGuiLayer.Current.PageExpeditionSuccess.m_overrideSuccessMusic = music;
        Logger.Verbose(LogLevel.Debug, $"Set success screen music to sound id {music}.");
    }

    private static void RestoreSuccessText()
    {
        if (s_storedSuccessText != string.Empty)
        {
            MainMenuGuiLayer.Current.PageExpeditionSuccess.m_header.SetText(s_storedSuccessText);
            s_storedSuccessText = string.Empty;
            LevelAPI.OnBuildStart -= RestoreSuccessText;
        }
    }
    
    private static void RestoreSuccessMusic()
    {
        if (s_shouldResetMusic)
        {
            MainMenuGuiLayer.Current.PageExpeditionSuccess.m_overrideSuccessMusic = 0; // 0 indicates the default music should be played
            s_shouldResetMusic = false;
            LevelAPI.OnBuildStart -= RestoreSuccessMusic;
        }
    }
}