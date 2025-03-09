using AWO.Modules.TerminalSerialLookup;
using GameData;
using GTFO.API.Extensions;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class SpecialHudTimerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SpecialHudTimer;

    private const string Timer = "[TIMER]";
    private const string Percent = "[PERCENT]";

    protected override void TriggerCommon(WEE_EventData e)
    {
        float duration = ResolveFieldsFallback(e.Duration, e.SpecialHudTimer.Duration);
        CoroutineManager.StartCoroutine(DoInteractionHud(e.SpecialHudTimer, duration).WrapToIl2Cpp());
    }

    static IEnumerator DoInteractionHud(WEE_SpecialHudTimer hud, float duration)
    {
        int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float time = 0.0f;
        float percentage;
        string msg = SerialLookupManager.ParseTextFragments(hud.Message);
        bool hasTags = msg.Contains(Timer) || msg.Contains(Percent);

        Queue<EventsOnTimerProgress> cachedProgressEvents = new(hud.EventsOnProgress.OrderBy(prEv => prEv.Progress));
        bool hasProgressEvents = cachedProgressEvents.Count > 0;
        double nextProgress = hasProgressEvents ? Math.Round(cachedProgressEvents.Peek().Progress, 3) : double.NaN;

        GuiManager.InteractionLayer.MessageVisible = true;
        GuiManager.InteractionLayer.MessageTimerVisible = hud.ShowTimeInProgressBar;
        yield return new WaitForSeconds(0.25f);

        while (time < duration)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || CheckpointManager.Current.m_stateReplicator.State.reloadCount > reloadCount)
            {
                GuiManager.InteractionLayer.MessageVisible = false;
                GuiManager.InteractionLayer.MessageTimerVisible = false;
                yield break;
            }

            percentage = time / duration;
            if (hud.ShowTimeInProgressBar)
            {
                GuiManager.InteractionLayer.SetMessageTimer(percentage);
            }

            if (hasTags)
            {
                var timeSpan = TimeSpan.FromSeconds(duration - time);
                string tagTime = $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
                string tagPercent = $"{(int)(percentage * 100)}%";
                string formattedMsg = msg.Replace(Timer, tagTime).Replace(Percent, tagPercent);

                GuiManager.InteractionLayer.SetMessage(formattedMsg, hud.Style, hud.Priority);
            }
            else
            {
                GuiManager.InteractionLayer.SetMessage(msg, hud.Style, hud.Priority);
            }
            
            time += Time.deltaTime;
            
            if (hasProgressEvents)
            {
                if (nextProgress <= Math.Round(percentage, 3))
                {
                    WOManager.CheckAndExecuteEventsOnTrigger(cachedProgressEvents.Dequeue().Events.ToIl2Cpp(), eWardenObjectiveEventTrigger.None);
                    if ((hasProgressEvents = cachedProgressEvents.Count > 0) == true)
                    {
                        nextProgress = Math.Round(cachedProgressEvents.Peek().Progress, 3);
                    }
                }                
            }

            yield return null;

            GuiManager.InteractionLayer.MessageVisible = true;
            GuiManager.InteractionLayer.MessageTimerVisible = hud.ShowTimeInProgressBar;
        }

        GuiManager.InteractionLayer.MessageVisible = false;
        GuiManager.InteractionLayer.MessageTimerVisible = false;

        while (cachedProgressEvents.Count > 0)
        {
            WOManager.CheckAndExecuteEventsOnTrigger(cachedProgressEvents.Dequeue().Events.ToIl2Cpp(), eWardenObjectiveEventTrigger.None);
        }

        WOManager.CheckAndExecuteEventsOnTrigger(hud.EventsOnDone.ToIl2Cpp(), eWardenObjectiveEventTrigger.None);
    }
}
