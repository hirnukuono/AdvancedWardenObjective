using AmorLib.Utils.Extensions;
using AWO.Modules.TSL;
using GTFO.API;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using SpecialHudType = AWO.Modules.WEE.WEE_SpecialHudTimer.SpecialHudType;

namespace AWO.Modules.WEE.Events;

internal sealed class SpecialHudTimerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SpecialHudTimer;

    public static readonly ConcurrentDictionary<int, Coroutine?> SpecialHuds = new();

    private const string Timer = "[TIMER]";
    private const string Percent = "[PERCENT]";

    protected override void OnSetup()
    {
        LevelAPI.OnLevelCleanup += OnLevelCleanup;
    }

    private void OnLevelCleanup()
    {
        SpecialHuds.ForEachValue(pSpecialHud => CoroutineManager.StopCoroutine(pSpecialHud));
        SpecialHuds.Clear();
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        var specHud = e.SpecialHudTimer ?? new();
        float duration = ResolveFieldsFallback(e.Duration, specHud.Duration);

        switch (specHud.Type)
        {
            case SpecialHudType.StartTimer: // start timed specialhud without an index
                if (duration <= 0.0f)
                {
                    LogError("Duration must be greater than 0 seconds!");
                    return;
                }
                LogDebug("Starting new Timed SpecialHud (no index, cannot be stopped once started)");
                CoroutineManager.StartCoroutine(DoSpecialHudTimed(specHud, duration).WrapToIl2Cpp());
                break;

            case SpecialHudType.StartIndexTimer: // start timed specialhud with index
                if (duration <= 0.0f)
                {
                    LogError("Duration must be greater than 0 seconds!");
                    return;
                }
                else if (!SpecialHuds.TryAdd(specHud.Index, null))
                {
                    LogError($"SpecialHud {specHud.Index} is already active...");
                    return;
                }
                LogDebug($"Starting Timed SpecialHud with index: {specHud.Index}");
                SpecialHuds[specHud.Index] = CoroutineManager.StartCoroutine(DoSpecialHudTimed(specHud, duration).WrapToIl2Cpp());
                break;

            case SpecialHudType.StartPersistent: // start persistent specialhud
                if (!SpecialHuds.TryAdd(specHud.Index, null))
                {
                    LogError($"SpecialHud {specHud.Index} is already active...");
                    return;
                }
                LogDebug($"Starting Persistent SpecialHud with index: {specHud.Index}");
                SpecialHuds[specHud.Index] = CoroutineManager.StartCoroutine(DoSpecialHudPersistent(specHud).WrapToIl2Cpp());
                break;

            case SpecialHudType.StopIndex: // stop specialhud with index
                if (!SpecialHuds.TryRemove(specHud.Index, out var loop))
                {
                    LogError($"No active SpecialHud with index {specHud.Index} was found!");
                    return;
                }
                LogDebug($"Stopping SpecialHud with index: {specHud.Index}");
                CoroutineManager.StopCoroutine(loop);
                GuiManager.InteractionLayer.MessageVisible = false;
                GuiManager.InteractionLayer.MessageTimerVisible = false;
                break;

            case SpecialHudType.StopAll: // stop all specialhuds with index
                SpecialHuds.ForEachValue(loop => CoroutineManager.StopCoroutine(loop));
                SpecialHuds.Clear();
                LogDebug("Stopping all indexed SpecialHuds");
                GuiManager.InteractionLayer.MessageVisible = false;
                GuiManager.InteractionLayer.MessageTimerVisible = false;
                break;
        }
    }

    static IEnumerator DoSpecialHudTimed(WEE_SpecialHudTimer hud, float duration)
    {
        int reloadCount = CheckpointManager.CheckpointUsage;
        float time = 0.0f;
        float percentage, invertPercent;
        string msg = SerialLookupManager.ParseTextFragments(hud.Message);
        bool hasTags = msg.Contains(Timer) || msg.Contains(Percent);

        Queue<EventsOnTimerProgress> cachedProgressEvents = new(hud.EventsOnProgress.OrderBy(prEv => prEv.Progress));
        bool hasProgressEvents = cachedProgressEvents.Count > 0;
        float nextProgress = hasProgressEvents ? cachedProgressEvents.Peek().Progress : float.NaN;

        GuiManager.InteractionLayer.MessageVisible = true;
        GuiManager.InteractionLayer.MessageTimerVisible = hud.ShowTimeInProgressBar;

        while (time <= duration)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || reloadCount < CheckpointManager.CheckpointUsage)
            {
                GuiManager.InteractionLayer.MessageVisible = false;
                GuiManager.InteractionLayer.MessageTimerVisible = false;
                yield break;
            }

            percentage = Mathf.Clamp01(time / duration);
            invertPercent = 1.0f - percentage;
            if (hud.ShowTimeInProgressBar)
            {
                GuiManager.InteractionLayer.SetMessageTimer(!hud.InvertProgress ? percentage : invertPercent);
            }

            if (!hasTags)
            {
                GuiManager.InteractionLayer.SetMessage(msg, hud.Style, hud.Priority);                
            }
            else
            {
                var timeSpan = TimeSpan.FromSeconds(!hud.InvertProgress ? duration - time : time);
                string tagTime = $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
                string tagPercent = $"{(!hud.InvertProgress ? percentage : invertPercent) * 100:F0}%";
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

        if (hud.Type == SpecialHudType.StartIndexTimer)
        {
            SpecialHuds.TryRemove(hud.Index, out _);
        }
    }

    static IEnumerator DoSpecialHudPersistent(WEE_SpecialHudTimer hud)
    {
        int reloadCount = CheckpointManager.CheckpointUsage;
        string msg = SerialLookupManager.ParseTextFragments(hud.Message);

        GuiManager.InteractionLayer.MessageVisible = true;
        GuiManager.InteractionLayer.MessageTimerVisible = false;

        while (true)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || reloadCount < CheckpointManager.CheckpointUsage)
            {
                GuiManager.InteractionLayer.MessageVisible = false;
                GuiManager.InteractionLayer.MessageTimerVisible = false;
                yield break;
            }

            GuiManager.InteractionLayer.SetMessage(msg, hud.Style, hud.Priority);            
            yield return null;

            GuiManager.InteractionLayer.MessageVisible = true;
            GuiManager.InteractionLayer.MessageTimerVisible = false;
        }
    }
}
