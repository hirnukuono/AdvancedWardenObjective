using AWO.Modules.WEE.Detours;
using AWO.Modules.WEE.Events;
using AWO.Modules.WEE.JsonInjects;
using AWO.Modules.WEE.Replicators;
using BepInEx.Logging;
using GameData;
using GTFO.API.Utilities;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using InjectLib.JsonNETInjection;
using LevelGeneration;
using Player;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace AWO.Modules.WEE;

internal static class WardenEventExt
{
    internal static readonly Dictionary<WEE_Type, BaseEvent> _EventsToTrigger = new();

    static WardenEventExt()
    {
        var eventTypes = AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly())
            .Where(x => !x.IsAbstract)
            .Where(x => x.IsAssignableTo(typeof(BaseEvent)));

        foreach (var type in eventTypes)
        {
            var instance = (BaseEvent)Activator.CreateInstance(type)!;
            if (_EventsToTrigger.TryGetValue(instance.EventType, out var existing))
            {
                Logger.Error($"Duplicate {nameof(BaseEvent.EventType)} detected!");
                Logger.Error($"With '{existing.Name}' and '{instance.Name}'");
                continue;
            }

            instance.Setup();
            _EventsToTrigger[instance.EventType] = instance;
        }
    }

    internal static void Initialize()
    {
        ClassInjector.RegisterTypeInIl2Cpp<OutsideDataReplicator>();
        ClassInjector.RegisterTypeInIl2Cpp<ScanPositionReplicator>();
        ClassInjector.RegisterTypeInIl2Cpp<ZoneLightReplicator>();

        JsonInjector.SetConverter(new EventTypeConverter());
        JsonInjector.SetConverter(new DimensionIndexConverter());
        JsonInjector.SetConverter(new LayerTypeConverter());
        JsonInjector.SetConverter(new LocalIndexConverter());
        JsonInjector.AddHandler(new EventDataHandler());
        JsonInjector.AddHandler(new TriggerDataHandler());

        WEE_EnumInjector.Inject();
        Detour_ExecuteEvent.Patch();
    }

    internal static void HandleEvent(WEE_Type type, WardenObjectiveEventData e, float currentDuration)
    {
        var weeData = e.GetWEEData();
        if (weeData == null)
        {
            Logger.Error($"WardenEvent Type is Extension ({type}), but it's not registered to any dataholder!");
            return;
        }

        if (!_EventsToTrigger.TryGetValue(type, out var eventInstance))
        {
            Logger.Error($"{type}Event does not exist in lookup!");
            return;
        }

        var globalIndices = Expand(weeData).ToList();
        foreach (var (dim, layer, zone) in globalIndices)
        {   
            if (!eventInstance.AllowArrayableGlobalIndex || globalIndices.Count == 1)
            {
                weeData.DimensionIndex = dim;
                weeData.Layer = layer;
                weeData.LocalIndex = zone;
                CoroutineManager.StartCoroutine(Handle(eventInstance, weeData, currentDuration).WrapToIl2Cpp());
                return;
            }            
            CoroutineManager.StartCoroutine(Handle(eventInstance, weeData.Clone(dim, layer, zone), currentDuration).WrapToIl2Cpp());
        }
    }

    private static IEnumerable<(eDimensionIndex, LG_LayerType, eLocalZoneIndex)> Expand(WEE_EventData e)
    {
        foreach (var d in e.ArrayableDimension.Values)
            foreach (var l in e.ArrayableLayer.Values)
                foreach (var z in e.ArrayableZone.Values)
                    yield return (d, l, z);
    }

    private static IEnumerator Handle(BaseEvent eventInstance, WEE_EventData e, float currentDuration)
    {
        float delay = Mathf.Max(e.Delay - currentDuration, 0f);
        if (delay > 0f)
        {
            int reloadCount = CheckpointManager.CheckpointUsage;
            yield return new WaitForSeconds(delay);
            if (reloadCount < CheckpointManager.CheckpointUsage)
            {
                Logger.Warn($"Delayed event {e.Type} aborted due to checkpoint reload");
                yield break;
            }
        }

        if (WorldEventManager.GetCondition(e.Condition.ConditionIndex) != e.Condition.IsTrue)
        {
            Logger.Verbose(LogLevel.Debug, $"Condition {e.Condition.ConditionIndex} is not met");
            yield break;
        }

        WOManager.DisplayWardenIntel(e.Layer, e.WardenIntel);

        if (e.Type != WEE_Type.ForcePlayPlayerDialogue)
        {
            if (e.DialogueID > 0u)
            {
                PlayerDialogManager.WantToStartDialog(e.DialogueID, -1, false, false);
            }
            if (e.SoundID > 0u)
            {
                WOManager.Current.m_sound.Post(e.SoundID, true);
                string line = e.SoundSubtitle;
                if (!string.IsNullOrWhiteSpace(line) && e.Type != WEE_Type.PlaySubtitles)
                {
                    GuiManager.PlayerLayer.ShowMultiLineSubtitle(line);
                }
            }
        }
        
        if (e.SubObjective.DoUpdate && e.Type != WEE_Type.MultiProgression)
        {
            WOManager.UpdateSyncCustomSubObjective(e.SubObjective.CustomSubObjectiveHeader, e.SubObjective.CustomSubObjective);
        }
        if (e.Fog.DoUpdate)
        {
            EnvironmentStateManager.AttemptStartFogTransition(e.Fog.FogSetting, e.Fog.FogTransitionDuration, e.DimensionIndex);
        }

        SafeInvoke.Invoke(eventInstance.Trigger, e);
    }
}
