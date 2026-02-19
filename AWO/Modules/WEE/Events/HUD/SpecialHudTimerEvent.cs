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

    public static readonly ConcurrentDictionary<int, SpecialHudItem> SpecialHuds = new();

    private const string Timer = "[TIMER]";
    private const string Percent = "[PERCENT]";

    public class SpecialHudItem
    {
        public Coroutine? Coroutine { get; set; }
        public float TimeModifier { get; set; }

        public void StartCoroutine(WEE_SpecialHudTimer hud, float duration)
        {
            var hudCoroutine = hud.Type == SpecialHudType.StartIndexTimer ? DoSpecialHudTimed(hud, duration) : DoSpecialHudPersistent(hud);
            Coroutine = CoroutineManager.StartCoroutine(hudCoroutine.WrapToIl2Cpp());
        }

        public void StopCoroutine()
        {
            if (Coroutine != null)
            {
                CoroutineManager.StopCoroutine(Coroutine);
                Coroutine = null;
            }
        }
    }

    protected override void OnSetup()
    {
        LevelAPI.OnLevelCleanup += OnLevelCleanup;
    }

    private void OnLevelCleanup()
    {
        SpecialHuds.ForEachValue(pSpecialHud => pSpecialHud.StopCoroutine());
        SpecialHuds.Clear();
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        var specHud = e.SpecialHudTimer ?? new();
        float duration = ResolveFieldsFallback(e.Duration, specHud.Duration);

        switch (specHud.Type)
        {
            case SpecialHudType.StartTimer: // start timed specialhud without an index
                if (duration <= 0f)
                {
                    LogError("Duration must be greater than 0 seconds!");
                    return;
                }
                LogDebug("Starting new Timed SpecialHud (no index, cannot be stopped once started)");
                CoroutineManager.StartCoroutine(DoSpecialHudTimed(specHud, duration).WrapToIl2Cpp());
                break;

            case SpecialHudType.StartIndexTimer: // start timed specialhud with index
                if (duration <= 0f)
                {
                    LogError("Duration must be greater than 0 seconds!");
                    return;
                }
                else if (!SpecialHuds.TryAdd(specHud.Index, new()))
                {
                    LogError($"SpecialHud {specHud.Index} is already active...");
                    return;
                }
                LogDebug($"Starting Timed SpecialHud with index: {specHud.Index}");
                SpecialHuds[specHud.Index].StartCoroutine(specHud, duration);
                break;

            case SpecialHudType.StartPersistent: // start persistent specialhud
                if (!SpecialHuds.TryAdd(specHud.Index, new()))
                {
                    LogError($"SpecialHud {specHud.Index} is already active...");
                    return;
                }
                LogDebug($"Starting Persistent SpecialHud with index: {specHud.Index}");
                SpecialHuds[specHud.Index].StartCoroutine(specHud, duration);
                break;

            case SpecialHudType.StopIndex: // stop specialhud with index
                if (!SpecialHuds.TryRemove(specHud.Index, out var hud))
                {
                    LogError($"No active SpecialHud with index {specHud.Index} was found!");
                    return;
                }
                LogDebug($"Stopping SpecialHud with index: {specHud.Index}");
                CoroutineManager.StopCoroutine(hud.Coroutine);
                GuiManager.InteractionLayer.MessageVisible = false;
                GuiManager.InteractionLayer.MessageTimerVisible = false;
                break;

            case SpecialHudType.StopAll: // stop all specialhuds with index
                SpecialHuds.ForEachValue(hud => CoroutineManager.StopCoroutine(hud.Coroutine));
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
        float time = 0f;
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

            percentage = Mathf.Clamp01(time / duration);
            invertPercent = 1f - percentage;
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

            time += Time.deltaTime;

            if (hud.HasIndex && SpecialHuds[hud.Index].TimeModifier != 0f)
            {
                time -= SpecialHuds[hud.Index].TimeModifier * (!hud.InvertProgress ? 1f : -1f);
                SpecialHuds[hud.Index].TimeModifier = 0f;
            }

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

        if (hud.HasIndex)
        {
            SpecialHuds.TryRemove(hud.Index, out _);
        }
    }

    public static float NormalizedPercent(float current, float min, float max)
    {
        if (min == max)
            return float.NaN;

        float clamp = Math.Clamp(current, min, max);
        return (clamp - min) / (max - min);
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
