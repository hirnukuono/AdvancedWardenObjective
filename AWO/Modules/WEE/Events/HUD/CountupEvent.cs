using AWO.Modules.WEE;
using GTFO.API.Utilities;
using System.Collections;
using UnityEngine;
using SNetwork;

namespace AWO.WEE.Events.HUD;

internal sealed class CountupEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.Countup;

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.CountdownStarted = Time.realtimeSinceStartup;
        CoroutineDispatcher.StartCoroutine(DoCountup(e.Countup, GetDuration(e)));
    }

    private static float GetDuration(WEE_EventData e)
    {
        if (e.Countup.Duration != 0.0f)
            return e.Countup.Duration;
        else if (e.Duration != 0.0f)
            return e.Duration;

        return e.Countup.Duration;
    }

    static IEnumerator DoCountup(WEE_CountupData cu, float duration)
    {
        int myReloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float myStartTime = EntryPoint.CountdownStarted;
        float count = cu.StartValue;
        string[] body = ParseCustomText(cu.CustomText.ToString(), duration);
        EntryPoint.TimerMods.SpeedModifier = cu.Speed;
        EntryPoint.TimerMods.CountupText = cu.CustomText;
        EntryPoint.TimerMods.TimerColor = cu.TimerColor;

        CoroutineManager.BlinkIn(GuiManager.PlayerLayer.m_objectiveTimer.gameObject);
        GuiManager.PlayerLayer.m_objectiveTimer.gameObject.active = true;
        GuiManager.PlayerLayer.m_objectiveTimer.m_titleText.text = cu.TimerText.ToString();

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

            GuiManager.PlayerLayer.m_objectiveTimer.m_timerText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(cu.TimerColor)}>{body[0]}{count.ToString($"F{cu.DecimalPoints}")}{body[1]}</color>";
            count += cu.Speed * Time.deltaTime;

            if (EntryPoint.TimerMods.TimeModifier != 0.0f) // time mod
            {
                count -= EntryPoint.TimerMods.TimeModifier;
                EntryPoint.TimerMods.TimeModifier = 0.0f;
            }
            if (EntryPoint.TimerMods.SpeedModifier != cu.Speed) // speed mod
                cu.Speed = EntryPoint.TimerMods.SpeedModifier;
            if (EntryPoint.TimerMods.CountupText != cu.CustomText) // text mod
            {
                cu.CustomText = EntryPoint.TimerMods.CountupText; 
                body = ParseCustomText(cu.CustomText.ToString(), duration);
            }
            if (EntryPoint.TimerMods.TimerColor != cu.TimerColor) // color mod
                cu.TimerColor = EntryPoint.TimerMods.TimerColor;

            yield return null;
        }

        CoroutineManager.BlinkOut(GuiManager.PlayerLayer.m_objectiveTimer.gameObject);
        GuiManager.PlayerLayer.m_objectiveTimer.gameObject.SetActive(false);
        foreach (var eventData in cu.EventsOnDone)
            if (SNet.IsMaster)
                WorldEventManager.ExecuteEvent(eventData);
    }

    private static string[] ParseCustomText(string custom, float d)
    {
        string tag = "[COUNTUP]";
        string max = "[CMAX]";

        if (!custom.Contains(tag))
            return new string[] { string.Empty, string.Empty };

        if (custom.Contains(max))
            custom = custom.Replace(max, d.ToString());

        return custom.Split(tag, 2);
    }
}