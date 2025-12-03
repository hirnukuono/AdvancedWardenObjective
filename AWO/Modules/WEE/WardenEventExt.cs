using AWO.Modules.WEE.JsonInjects;
using AWO.Modules.WEE.Detours;
using AWO.Modules.WEE.Events;
using AWO.Modules.WEE.Replicators;
using GameData;
using Il2CppInterop.Runtime.Injection;
using InjectLib.JsonNETInjection;
using Player;
using System.Collections;
using UnityEngine;
using BepInEx.Logging;
using LevelGeneration;
using AWO.Jsons;

namespace AWO.Modules.WEE;

internal static class WardenEventExt
{
    internal static readonly Dictionary<WEE_Type, BaseEvent> _EventsToTrigger = new();

    static WardenEventExt()
    {
        var eventTypes = typeof(BaseEvent).Assembly.GetTypes()
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
        JsonInjector.AddHandler(new EventDataHandler());
        JsonInjector.AddHandler(new TriggerDataHandler());
        WEE_EnumInjector.Inject();
        Detour_ExecuteEvent.Patch();
    }

    internal static void HandleEvent(WEE_Type type, WardenObjectiveEventData e, float currentDuration)
    {
       // Logger.Debug($"We got Type {type} on WardenEventExt event");

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

        if (!eventInstance.WhitelistArrayableGlobalIndex)
        {
            CoroutineManager.StartCoroutine(Handle(eventInstance, weeData, currentDuration).WrapToIl2Cpp());
            return;
        }

        foreach (var (dim, layer, zone) in Expand(weeData.ArrayableDimension, weeData.ArrayableLayer, weeData.ArrayableZone))
        {
            var newData = weeData;
            newData.DimensionIndex = dim;
            newData.Layer = layer;
            newData.LocalIndex= zone;
            CoroutineManager.StartCoroutine(Handle(eventInstance, newData, currentDuration).WrapToIl2Cpp());
        }
    }

    private static IEnumerable<(eDimensionIndex, LG_LayerType, eLocalZoneIndex)> Expand(Arrayable<eDimensionIndex> arrDim, Arrayable<LG_LayerType> arrlayer, Arrayable<eLocalZoneIndex> arrZone)
    {
        foreach (var d in arrDim.Values)
            foreach (var l in arrlayer.Values)
                foreach (var z in arrZone.Values)
                    yield return (d, l, z);
    }

    private static IEnumerator Handle(BaseEvent eventInstance, WEE_EventData e, float currentDuration)
    {
        float delay = Mathf.Max(e.Delay - currentDuration, 0f);
        if (delay > 0f)
        {
            int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
            yield return new WaitForSeconds(delay);
            if (reloadCount < CheckpointManager.Current.m_stateReplicator.State.reloadCount)
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

        eventInstance.Trigger(e);
    }
}
