using AWO.WEE.Events;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events.Level;
internal sealed class SetSuccessScreenEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetSuccessScreen;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (e.SuccessScreen.Type == 0)
            SetScreen(e.SuccessScreen.CustomSuccessScreen.ToString());
        else
            CoroutineManager.StartCoroutine(FakeScreen(e).WrapToIl2Cpp());
    }
    
    private static void SetScreen(string pageResourcePath)
    {
        if (pageResourcePath == string.Empty)
        {
            Logger.Error("SetSuccessScreen - Invalid CustomSuccessScreen!");
            return;
        }

        var menuGUI = MainMenuGuiLayer.Current;
        try
        {
            menuGUI.PageCustomExpeditionSuccess = menuGUI.AddPage(eCM_MenuPage.CMP_EXPEDITION_SUCCESS, pageResourcePath);
            Logger.Debug($"SetSuccessScreen - CustomSuccessScreen changed to {pageResourcePath}");
        }
        catch
        {
            Logger.Error($"SetSuccessScreen - CustomSuccessScreen asset {pageResourcePath} not found!");
        }
    }
    
    static IEnumerator FakeScreen(WEE_EventData e)
    {
        Logger.Debug("SetSuccessScreen - Enabling fake end screen... Disabled map and menu toggle");
        FocusStateManager.EnterMenu(e.SuccessScreen.FakeEndScreen, force: true);
        FocusStateManager.MapToggleAllowed = false;
        FocusStateManager.MenuToggleAllowed = false;

        yield return new WaitForSeconds(GetDuration(e));

        Logger.Debug("SetSuccessScreen - Disabling fake end screen... Enabled map and menu toggle");
        FocusStateManager.ExitMenu();
        FocusStateManager.ChangeState(eFocusState.FPS, force: true);
        FocusStateManager.MapToggleAllowed = true;
        FocusStateManager.MenuToggleAllowed = true;
    }

    private static float GetDuration(WEE_EventData e)
    {
        if (e.SuccessScreen.Duration != 0.0f)
            return e.SuccessScreen.Duration;
        else if (e.Duration != 0.0f)
            return e.Duration;

        return e.SuccessScreen.Duration;
    }
}