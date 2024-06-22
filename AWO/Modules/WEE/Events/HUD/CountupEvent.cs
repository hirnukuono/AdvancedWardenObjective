using AWO.Modules.WEE;
using GTFO.API.Utilities;
using System.Collections;
using UnityEngine;
using SNetwork;
using TMPro;

namespace AWO.WEE.Events.HUD;

internal sealed class CountupEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.Countup;

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.CountdownStarted = Time.realtimeSinceStartup;
        EntryPoint.TimerModifier = 0.0f;
        CoroutineDispatcher.StartCoroutine(DoCountup(e));
    }

    static IEnumerator DoCountup(WEE_EventData e)
    {
        int myReloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float myStartTime = EntryPoint.CountdownStarted;
        var cu = e.Countup;
        float duration = e.Duration;
        float speed = e.Speed;
        float count = 0.0f;
        EntryPoint.SpeedModifier = speed;

        GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(true, true);
        GuiManager.PlayerLayer.m_objectiveTimer.SetTimerTextEnabled(true);
        GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerTitle(cu.TimerText.ToString());

        string head, tail;
        string tag = "[COUNTUP]";
        int tagIndex = cu.CustomText.ToString().IndexOf(tag);

        if (tagIndex == -1)
        {
            head = string.Empty;
            tail = string.Empty;
        }
        else
        {
            head = cu.CustomText.ToString().Substring(0, tagIndex);
            tail = cu.CustomText.ToString().Substring(tagIndex + tag.Length);
        }

        TextMeshPro myTimerText = new()
        {
            enabled = true,
            color = cu.TimerColor,
            text = $"{head}{count.ToString($"F{cu.DecimalPoints}")}{tail}"
        };
        TMP_UpdateManager.RegisterTextObjectForUpdate(myTimerText);
        //TextmeshRebuildManager.RegisterForRebuild(myTimerText);

        while (count <= duration)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
                yield break;
            if (myStartTime < EntryPoint.CountdownStarted)
                yield break; // someone has started a new countup while we were stuck here, exit
            if (CheckpointManager.Current.m_stateReplicator.State.reloadCount > myReloadCount)
            {
                GuiManager.PlayerLayer.m_objectiveTimer.gameObject.active = false;
                yield break; // checkpoint has been used
            }

            myTimerText.text = $"{head}{count.ToString($"F{cu.DecimalPoints}")}{tail}";
            TMP_UpdateManager.RegisterTextObjectForUpdate(myTimerText);
            count += speed * Time.deltaTime;

            if (EntryPoint.TimerModifier != 0.0f)
            {
                count -= EntryPoint.TimerModifier;
                EntryPoint.TimerModifier = 0.0f;
            }
            if (EntryPoint.SpeedModifier != speed)
                speed = EntryPoint.SpeedModifier;

            yield return null;
        }

        GuiManager.PlayerLayer.m_objectiveTimer.gameObject.active = false;
        foreach (var eventData in cu.EventsOnDone)
            if (SNet.IsMaster)
                WorldEventManager.ExecuteEvent(eventData);
    }
}