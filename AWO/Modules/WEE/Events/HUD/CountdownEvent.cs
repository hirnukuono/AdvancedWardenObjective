using AmorLib.Utils.JsonElementConverters;
using AWO.Modules.TSL;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class CountdownEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.Countdown;

    private static PUI_ObjectiveTimer ObjHudTimer => GuiManager.PlayerLayer.m_objectiveTimer;

    protected override void TriggerCommon(WEE_EventData e)
    {
        float duration = ResolveFieldsFallback(e.Duration, e.Countdown?.Duration ?? 0f);
        if (duration <= 0f)
            LogWarning("Duration should generally be more than 0 seconds");

        EntryPoint.Coroutines.CountdownStarted = Time.realtimeSinceStartup; // i keep fucking this up. we need to refresh the time **before** starting the corouinte
        CoroutineManager.StartCoroutine(DoCountdown(e.Countdown ?? new(), duration).WrapToIl2Cpp());
    }

    static IEnumerator DoCountdown(WEE_CountdownData cd, float duration)
    {
        int reloadCount = CheckpointManager.CheckpointUsage;
        float startTime = EntryPoint.Coroutines.CountdownStarted;
        float time = 0f;        
        LocaleText timerTitle = SerialLookupManager.ParseLocaleText(cd.TimerText);
        Color color = cd.TimerColor;

        EntryPoint.TimerMods.TimeModifier = 0f;        
        EntryPoint.TimerMods.TimerTitleText = timerTitle;
        EntryPoint.TimerMods.TimerColor = color;

        Queue<EventsOnTimerProgress> cachedProgressEvents = new(cd.EventsOnProgress.OrderBy(prEv => prEv.Progress));
        bool hasProgressEvents = cachedProgressEvents.Count > 0;
        float nextProgress = hasProgressEvents ? cachedProgressEvents.Peek().Progress : float.NaN;

        ObjHudTimer.SetTimerActive(true, true);
        ObjHudTimer.UpdateTimerTitle(timerTitle);
        UpdateTimerText(time, duration, color, cd.CanShowHours);

        while (time <= duration)
        {
            if (startTime < EntryPoint.Coroutines.CountdownStarted)
            {
                // someone has started a new countdown while we were here, exit
                yield break; 
            }
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || reloadCount < CheckpointManager.CheckpointUsage)
            {
                // checkpoint has been used
                ObjHudTimer.SetTimerActive(false, false);
                ObjHudTimer.SetTimerTextEnabled(false);
                yield break;
            }

            if (hasProgressEvents)
            {
                if (nextProgress <= NormalizedPercent(time, 0f, duration))
                {
                    ExecuteWardenEvents(cachedProgressEvents.Dequeue().Events);
                    if ((hasProgressEvents = cachedProgressEvents.Count > 0) == true)
                    {
                        nextProgress = cachedProgressEvents.Peek().Progress;
                    }
                }
            }

            UpdateTimerText(time, duration, color, cd.CanShowHours);
            time += Time.deltaTime;

            #region TIMER_MODS
            if (EntryPoint.TimerMods.TimeModifier != 0f) // time mod
            {
                time -= EntryPoint.TimerMods.TimeModifier;
                EntryPoint.TimerMods.TimeModifier = 0f;
            }
            if (EntryPoint.TimerMods.TimerTitleText != timerTitle) // title text mod
            {
                timerTitle = EntryPoint.TimerMods.TimerTitleText;
                GuiManager.PlayerLayer.m_objectiveTimer.m_timerText.text = timerTitle;
            }
            if (EntryPoint.TimerMods.TimerColor != color) // color mod
            {
                color = EntryPoint.TimerMods.TimerColor;
            }
            #endregion

            yield return null;
        }
        
        while (cachedProgressEvents.Count > 0)
        {
            ExecuteWardenEvents(cachedProgressEvents.Dequeue().Events);
        }
        ExecuteWardenEvents(cd.EventsOnDone);

        if (GameStateManager.CurrentStateName != eGameStateName.InLevel || startTime < EntryPoint.Coroutines.CountdownStarted)
        {
            // catch for if new timer started at the last moment
            yield break; 
        }
        ObjHudTimer.SetTimerActive(false, true);        
    }

    public static void UpdateTimerText(float time, float duration, Color color, bool showHours)
    {
        ObjHudTimer.SetTimerTextEnabled(true);
        float remainder = Math.Max(duration - time, 0f);
        var timeSpan = TimeSpan.FromSeconds(remainder);

        ObjHudTimer.m_timerText.color = color;
        ObjHudTimer.m_timerText.text = showHours && timeSpan.TotalSeconds >= 3600f ? $"{(int)timeSpan.TotalHours:D1}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}" : $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
    }

    public static float NormalizedPercent(float current, float min, float max)
    {
        if (min == max) 
            return float.NaN;

        float clamp = Math.Clamp(current, min, max);
        return (clamp - min) / (max - min);
    }
}
