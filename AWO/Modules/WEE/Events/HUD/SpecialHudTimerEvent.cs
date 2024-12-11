using FluffyUnderware.Curvy.Utils;
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
        float duration = ResolveFieldFallback(e.Duration, e.SpecialHudTimer.Duration);
        CoroutineManager.StartCoroutine(DoInteractionHud(e.SpecialHudTimer, duration).WrapToIl2Cpp());
    }

    static IEnumerator DoInteractionHud(WEE_SpecialHudTimer hud, float duration)
    {
        int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float time = 0.0f;
        float percentage;
        string msg = hud.Message.ToString();
        bool hasTags = msg.Contains(Timer) || msg.Contains(Percent);

        List<EventsOnTimerProgress> cachedProgressEvents = new(hud.EventsOnProgress);
        bool hasProgressEvents = cachedProgressEvents.Count > 0;

        GuiManager.InteractionLayer.MessageVisible = true;
        GuiManager.InteractionLayer.MessageTimerVisible = hud.ShowTimeInProgressBar;
        yield return new WaitForSeconds(0.25f);

        while (time < duration)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
            {
                yield break;
            }
            if (CheckpointManager.Current.m_stateReplicator.State.reloadCount > reloadCount)
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
                string tagTime = $"{(int)TimeSpan.FromSeconds(duration - time).TotalMinutes:D2}:{TimeSpan.FromSeconds(duration - time).Seconds:D2}";
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
                foreach (var progressEvents in cachedProgressEvents.Where(prEv => !prEv.HasBeenActivated && prEv.Progress.Approximately(percentage)))
                {
                    WOManager.CheckAndExecuteEventsOnTrigger(progressEvents.Events.ToIl2Cpp(), eWardenObjectiveEventTrigger.None);
                    progressEvents.SetActivated();
                }
                hasProgressEvents = cachedProgressEvents.Any(prEv => !prEv.HasBeenActivated);
            }

            yield return null;

            GuiManager.InteractionLayer.MessageVisible = true;
            GuiManager.InteractionLayer.MessageTimerVisible = hud.ShowTimeInProgressBar;
        }

        GuiManager.InteractionLayer.MessageVisible = false;
        GuiManager.InteractionLayer.MessageTimerVisible = false;
        WOManager.CheckAndExecuteEventsOnTrigger(hud.EventsOnDone.ToIl2Cpp(), eWardenObjectiveEventTrigger.None);
    }
}
