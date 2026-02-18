using AmorLib.Utils.JsonElementConverters;
using AWO.Modules.TSL;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class CountupEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.Countup;

    private static PUI_ObjectiveTimer ObjHudTimer => GuiManager.PlayerLayer.m_objectiveTimer;

    private const string Tag = "[COUNTUP]";
    private const string Max = "[CMAX]";

    protected override void TriggerCommon(WEE_EventData e)
    {
        float duration = ResolveFieldsFallback(e.Duration, e.Countup?.Duration ?? 0f);
        if (duration <= 0f)
            LogWarning("Duration should generally be more than 0 seconds");

        EntryPoint.Coroutines.CountdownStarted = Time.realtimeSinceStartup; // i keep fucking this up. we need to refresh the time **before** starting the corouinte
        CoroutineManager.StartCoroutine(DoCountup(e.Countup ?? new(), duration).WrapToIl2Cpp());
    }

    static IEnumerator DoCountup(WEE_CountupData cu, float duration)
    {
        int reloadCount = CheckpointManager.CheckpointUsage;
        float startTime = EntryPoint.Coroutines.CountdownStarted;
        float count = cu.StartValue;
        float speed = cu.Speed;
        LocaleText titleText = SerialLookupManager.ParseLocaleText(cu.TimerText);
        LocaleText customText = SerialLookupManager.ParseLocaleText(cu.CustomText);
        string[] body = ParseCustomText(customText, duration);
        Color color = cu.TimerColor;
        string htmlColor = ColorUtility.ToHtmlStringRGB(color);

        EntryPoint.TimerMods.TimeModifier = 0f;
        EntryPoint.TimerMods.SpeedModifier = speed;
        EntryPoint.TimerMods.TimerTitleText = titleText;
        EntryPoint.TimerMods.TimerBodyText = customText;
        EntryPoint.TimerMods.TimerColor = color;

        Queue<EventsOnTimerProgress> cachedProgressEvents = new(cu.EventsOnProgress.OrderBy(prEv => prEv.Progress));
        bool hasProgressEvents = cachedProgressEvents.Count > 0;
        float nextProgress = hasProgressEvents ? cachedProgressEvents.Peek().Progress : float.NaN;

        ObjHudTimer.SetTimerActive(true, true);
        ObjHudTimer.UpdateTimerTitle(titleText);
        ObjHudTimer.m_timerText.text = $"<color=#{htmlColor}>{body[0]}{count.ToString($"F{cu.DecimalPoints}")}{body[1]}</color>";
        yield return new WaitForSeconds(0.25f);

        while (count <= duration)
        {
            if (startTime < EntryPoint.Coroutines.CountdownStarted)
            {
                // someone has started a new countup while we were here, exit
                yield break; 
            }
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || reloadCount < CheckpointManager.CheckpointUsage)
            {
                ObjHudTimer.SetTimerActive(false, false);
                yield break; // checkpoint has been used
            }

            if (hasProgressEvents)
            {
                if (nextProgress <= NormalizedPercent(count, cu.StartValue, duration))
                {
                    ExecuteWardenEvents(cachedProgressEvents.Dequeue().Events);
                    if ((hasProgressEvents = cachedProgressEvents.Count > 0) == true)
                    {
                        nextProgress = cachedProgressEvents.Peek().Progress;
                    }
                }
            }

            ObjHudTimer.m_timerText.text = $"<color=#{htmlColor}>{body[0]}{count.ToString($"F{cu.DecimalPoints}")}{body[1]}</color>";
            count += speed * Time.deltaTime;

            #region TIMER_MODS
            if (EntryPoint.TimerMods.TimeModifier != 0f) // time mod
            {
                count += EntryPoint.TimerMods.TimeModifier;
                EntryPoint.TimerMods.TimeModifier = 0f;
            }
            if (EntryPoint.TimerMods.SpeedModifier != speed) // speed mod
            {
                speed = EntryPoint.TimerMods.SpeedModifier;
            }
            if (EntryPoint.TimerMods.TimerTitleText != titleText) // title text mod
            {
                titleText = EntryPoint.TimerMods.TimerTitleText;
                ObjHudTimer.m_titleText.text = titleText;
            }
            if (EntryPoint.TimerMods.TimerBodyText != customText) // body text mod
            {
                customText = EntryPoint.TimerMods.TimerBodyText;
                body = ParseCustomText(customText, duration);
            }
            if (EntryPoint.TimerMods.TimerColor != color) // color mod
            {
                color = EntryPoint.TimerMods.TimerColor;
                htmlColor = ColorUtility.ToHtmlStringRGB(color);
            }
            #endregion

            yield return null;
        }
        
        while (cachedProgressEvents.Count > 0) 
        {
            ExecuteWardenEvents(cachedProgressEvents.Dequeue().Events);
        }
        ExecuteWardenEvents(cu.EventsOnDone);

        if (GameStateManager.CurrentStateName != eGameStateName.InLevel || startTime < EntryPoint.Coroutines.CountdownStarted)
        {
            // catch for if new timer started at the last moment
            yield break; 
        }
        ObjHudTimer.SetTimerActive(false, true);        
    }

    private static string[] ParseCustomText(string custom, float duration)
    {
        if (!custom.Contains(Tag))
            return new string[] { string.Empty, string.Empty };

        if (custom.Contains(Max))
            custom = custom.Replace(Max, duration.ToString());

        return custom.Split(Tag, 2);
    }

    private static float NormalizedPercent(float current, float min, float max)
    {
        if (min == max) 
            return float.NaN;

        float clamp = Math.Clamp(current, min, max);
        return (clamp - min) / (max - min);
    }
}