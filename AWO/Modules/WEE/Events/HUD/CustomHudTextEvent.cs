using AK;
using AWO.Modules.WEE;
using UnityEngine;

namespace AWO.WEE.Events.HUD;

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
            GuiManager.PlayerLayer.m_objectiveTimer.m_titleText.text = e.CustomHudText.Title.ToString();
            GuiManager.PlayerLayer.m_objectiveTimer.m_timerText.text = e.CustomHudText.Body.ToString();
        }
        else
        {
            CoroutineManager.BlinkOut(GuiManager.PlayerLayer.m_objectiveTimer.gameObject);
        }
    }
}
