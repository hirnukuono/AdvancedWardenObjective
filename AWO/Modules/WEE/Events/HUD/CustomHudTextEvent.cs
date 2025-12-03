using AK;
using AmorLib.Events;
using AWO.Modules.TSL;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class CustomHudTextEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.CustomHudText;
    private static PUI_ObjectiveTimer ObjHudTimer => GuiManager.PlayerLayer.m_objectiveTimer;

    protected override void OnSetup()
    {
        SNetEvents.OnCheckpointReload += OnCheckpointReload;
    }

    private void OnCheckpointReload() // should kill the timer hud
    {
        if (ObjHudTimer != null && ObjHudTimer.enabled)
        {
            ObjHudTimer.m_titleText.text = string.Empty;
            ObjHudTimer.m_timerText.text = string.Empty;
            ObjHudTimer.SetTimerActive(false, false);
            ObjHudTimer.SetTimerTextEnabled(false);
        }
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.Coroutines.CountdownStarted = Time.realtimeSinceStartup;
        ObjHudTimer.m_timerSoundPlayer.Post(EVENTS.STINGER_SUBOBJECTIVE_COMPLETE, true);

        if (e.Enabled)
        {    
            CoroutineManager.BlinkIn(ObjHudTimer.gameObject);
            ObjHudTimer.m_titleText.text = SerialLookupManager.ParseTextFragments(e.CustomHudText.Title);
            ObjHudTimer.m_timerText.text = SerialLookupManager.ParseTextFragments(e.CustomHudText.Body);
        }
        else
        {
            CoroutineManager.BlinkOut(ObjHudTimer.gameObject);
        }
    }
}
