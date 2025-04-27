using AK;
using AWO.Modules.TSL;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class CustomHudTextEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.CustomHudText;
    private static PUI_ObjectiveTimer ObjHudTimer => GuiManager.PlayerLayer.m_objectiveTimer;

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
