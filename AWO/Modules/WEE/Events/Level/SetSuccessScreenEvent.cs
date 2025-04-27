using BepInEx.Logging;
using System.Collections;
using UnityEngine;
using ScreenType = AWO.Modules.WEE.WEE_SetSuccessScreen.ScreenType;

namespace AWO.Modules.WEE.Events;
internal sealed class SetSuccessScreenEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetSuccessScreen;

    protected override void TriggerCommon(WEE_EventData e)
    {
        switch (e.SuccessScreen.Type)
        {
            case ScreenType.SetSuccessScreen:
                SetScreen(e.SuccessScreen.CustomSuccessScreen);
                break;
            case ScreenType.FlashFakeScreen:
                CoroutineManager.StartCoroutine(FakeScreen(e).WrapToIl2Cpp());
                break;
        }
    }
    
    private static void SetScreen(string pageResourcePath)
    {
        if (pageResourcePath == string.Empty)
        {
            Logger.Error("[SetSuccessScreen] Invalid CustomSuccessScreen!");
            return;
        }

        try
        {
            MainMenuGuiLayer.Current.AddPage(eCM_MenuPage.CMP_EXPEDITION_SUCCESS, pageResourcePath);
            Logger.Dev(LogLevel.Debug, $"CustomSuccessScreen should now be changed to {pageResourcePath}");
        }
        catch
        {
            Logger.Error($"[SetSuccessScreen] CustomSuccessScreen asset {pageResourcePath} not found!");
        }
    }
    
    static IEnumerator FakeScreen(WEE_EventData e)
    {
        Logger.Dev(LogLevel.Debug, "Enabling fake end screen... Disabled map and menu toggle");
        FocusStateManager.EnterMenu(e.SuccessScreen.FakeEndScreen, force: true);
        FocusStateManager.MapToggleAllowed = false;
        FocusStateManager.MenuToggleAllowed = false;

        yield return new WaitForSeconds(e.Duration);

        Logger.Dev(LogLevel.Debug, "Disabling fake end screen... Enabled map and menu toggle");
        FocusStateManager.ExitMenu();
        FocusStateManager.ChangeState(eFocusState.FPS, force: true);
        FocusStateManager.MapToggleAllowed = true;
        FocusStateManager.MenuToggleAllowed = true;
    }
}