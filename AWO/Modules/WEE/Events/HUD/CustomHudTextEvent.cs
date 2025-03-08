using AK;
using AWO.Modules.TerminalSerialLookup;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class CustomHudTextEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.CustomHudText;

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.Coroutines.CountdownStarted = Time.realtimeSinceStartup;
        GuiManager.PlayerLayer.m_objectiveTimer.m_timerSoundPlayer.Post(EVENTS.STINGER_SUBOBJECTIVE_COMPLETE, true);

        if (e.Enabled)
        {    
            CoroutineManager.BlinkIn(GuiManager.PlayerLayer.m_objectiveTimer.gameObject);
            GuiManager.PlayerLayer.m_objectiveTimer.m_titleText.text = SerialLookupManager.ParseTextFragments(e.CustomHudText.Title);
            GuiManager.PlayerLayer.m_objectiveTimer.m_timerText.text = SerialLookupManager.ParseTextFragments(e.CustomHudText.Body);
        }
        else
        {
            CoroutineManager.BlinkOut(GuiManager.PlayerLayer.m_objectiveTimer.gameObject);
        }
    }
}
