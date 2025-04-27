using AWO.Modules.TSL;
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
        if (duration <= 0.0f)
        {
            LogError("Duration must be greater than 0!");
            return;
        }

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
        float nextProgress = hasProgressEvents ? cachedProgressEvents.Peek().Progress : float.NaN;

        GuiManager.InteractionLayer.MessageVisible = true;
        GuiManager.InteractionLayer.MessageTimerVisible = hud.ShowTimeInProgressBar;

        while (time <= duration)
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

            if (!hasTags)
            {
                GuiManager.InteractionLayer.SetMessage(msg, hud.Style, hud.Priority);                
            }
            else
            {
                var timeSpan = TimeSpan.FromSeconds(duration - time);
                string tagTime = $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
                string tagPercent = $"{percentage * 100:F0}%";
                string formattedMsg = msg.Replace(Timer, tagTime, StringComparison.Ordinal).Replace(Percent, tagPercent, StringComparison.Ordinal);

                GuiManager.InteractionLayer.SetMessage(formattedMsg, hud.Style, hud.Priority);
            }
            
            if (hasProgressEvents)
            {
                if (nextProgress <= percentage)
                {
                    ExecuteWardenEvents(cachedProgressEvents.Dequeue().Events);
                    if ((hasProgressEvents = cachedProgressEvents.Count > 0) == true)
                    {
                        nextProgress = cachedProgressEvents.Peek().Progress;
                    }
                }                
            }

            time += Time.deltaTime;
            yield return null;

            GuiManager.InteractionLayer.MessageVisible = true;
            GuiManager.InteractionLayer.MessageTimerVisible = hud.ShowTimeInProgressBar;
        }

        GuiManager.InteractionLayer.MessageVisible = false;
        GuiManager.InteractionLayer.MessageTimerVisible = false;

        while (cachedProgressEvents.Count > 0)
        {
            ExecuteWardenEvents(cachedProgressEvents.Dequeue().Events);
        }
        ExecuteWardenEvents(hud.EventsOnDone);
    }
}
