using AK;
using AWO.Modules.WEE;
using System.Collections;
using UnityEngine;

namespace AWO.WEE.Events.HUD;

internal sealed class CustomHudTextEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.CustomHudText;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (e.Enabled)
        {
            EntryPoint.Coroutines.CountdownStarted = Time.realtimeSinceStartup;
            CoroutineManager.StartCoroutine(SetHud(e.CustomHudText).WrapToIl2Cpp());
        }
        else
        {
            CoroutineManager.StopCoroutine(SetHud(e.CustomHudText).WrapToIl2Cpp());
            ClearHud();
        }
    }

    static IEnumerator SetHud(WEE_CustomHudText hud)
    {
        int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        WaitForSeconds delay = new(0.5f);

        CoroutineManager.BlinkIn(GuiManager.PlayerLayer.m_objectiveTimer.gameObject);
        GuiManager.PlayerLayer.m_objectiveTimer.m_timerSoundPlayer.Post(EVENTS.STINGER_SUBOBJECTIVE_COMPLETE, true);
        GuiManager.PlayerLayer.m_objectiveTimer.m_titleText.text = hud.Title.ToString();
        GuiManager.PlayerLayer.m_objectiveTimer.m_timerText.text = hud.Body.ToString();

        while (true)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
                yield break;
            if (CheckpointManager.Current.m_stateReplicator.State.reloadCount > reloadCount)
            {
                GuiManager.PlayerLayer.m_objectiveTimer.gameObject.active = false;
                yield break;
            }

            yield return delay;
        }
    }

    private static void ClearHud()
    {
        CoroutineManager.BlinkOut(GuiManager.PlayerLayer.m_objectiveTimer.gameObject);
        GuiManager.PlayerLayer.m_objectiveTimer.m_timerSoundPlayer.Post(EVENTS.STINGER_SUBOBJECTIVE_COMPLETE, true);
    }
}
