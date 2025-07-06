using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using GameData;
using GTFO.API.Extensions;
using LevelGeneration;
using Player;
using SNetwork;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal abstract class BaseEvent
{
    public string Name { get; private set; } = string.Empty;
    protected PlayerAgent LocalPlayer { get; private set; } = new();
    protected static bool IsMaster => SNet.IsMaster;
    protected static bool HasMaster => SNet.HasMaster;
    public static System.Random MasterRand { get; } = new(Guid.NewGuid().GetHashCode());
    public abstract WEE_Type EventType { get; }

    public void Setup()
    {
        Name = GetType().Name;    
        OnSetup();
    }

    public void Trigger(WEE_EventData e)
    {
        if (!PlayerManager.HasLocalPlayerAgent())
        {
            Logger.Error($"Doesn't have LocalPlayer while triggering {Name}, wtf?");
            return;
        }

        LocalPlayer = PlayerManager.GetLocalPlayerAgent();
        TriggerCommon(e);

        if (IsMaster) TriggerMaster(e);
        else TriggerClient(e);
    }

    protected virtual void OnSetup() { }
    protected virtual void TriggerCommon(WEE_EventData e) { }
    protected virtual void TriggerClient(WEE_EventData e) { }
    protected virtual void TriggerMaster(WEE_EventData e) { }

    protected void LogInfo(string msg) => Logger.Info($"[{Name}] {msg}");
    protected void LogInfo(BepInExInfoLogInterpolatedStringHandler handler)
    {
        if (handler.Enabled)
        {
            Logger.Info($"[{Name}] {handler}");
        }
    }

    protected void LogDebug(string msg) => Logger.Debug($"[{Name}] {msg}");
    protected void LogDebug(BepInExDebugLogInterpolatedStringHandler handler)
    {
        if (handler.Enabled)
        {
            Logger.Debug($"[{Name}] {handler}");
        }
    }

    protected void LogWarning(string msg) => Logger.Warn($"[{Name}] {msg}");
    protected void LogWarning(BepInExWarningLogInterpolatedStringHandler handler)
    {
        if (handler.Enabled)
        {
            Logger.Warn($"[{Name}] {handler}");
        }
    }

    protected void LogError(string msg) => Logger.Error($"[{Name}] {msg}");
    protected void LogError(BepInExErrorLogInterpolatedStringHandler handler)
    {
        if (handler.Enabled)
        {
            Logger.Error($"[{Name}] {handler}");
        }
    }

    public bool TryGetZone(WEE_EventData e, [NotNullWhen(true)] out LG_Zone? zone)
    {
        Logger.Verbose(LogLevel.Debug, $"Searching for ({e.DimensionIndex}, {e.Layer}, {e.LocalIndex}) in level...");
        if (Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out zone))
        {
            return true;
        }
        LogError("Unable to find zone in level!");
        return false;
    }

    public bool TryGetZoneEntranceSecDoor(WEE_EventData e, [NotNullWhen(true)] out LG_SecurityDoor? door)
    {
        if (TryGetZone(e, out var zone))
        {
            return TryGetZoneEntranceSecDoor(zone, out door);
        }
        door = null;
        return false;
    }

    public bool TryGetZoneEntranceSecDoor(LG_Zone zone, [NotNullWhen(true)] out LG_SecurityDoor? door)
    {
        door = zone.m_sourceGate?.SpawnedDoor.TryCast<LG_SecurityDoor>();
        if (door != null)
        { 
            return true;
        }
        LogError("Unable to find entrance/source security door for zone!");
        return false;
    }

    public bool TryGetTerminalFromZone(WEE_EventData e, int index, [NotNullWhen(true)] out LG_ComputerTerminal? terminal)
    {
        if (TryGetZone(e, out var zone))
        {
            terminal = zone.TerminalsSpawnedInZone[index];
            return terminal != null;
        }

        LogError($"Unable to find terminal {index} in {e.LocalIndex}!");
        terminal = null;
        return false;
    }

    public bool IsValidAreaIndex(int areaIndex, LG_Zone zone)
    {
        var areas = zone.m_areas;
        if (areaIndex < 0 || areaIndex >= areas.Count)
        {
            LogError($"Invalid area index ({areaIndex}) for zone");
            return false;
        }

        return true;
    }

    public S ResolveFieldsFallback<S>(S value, S nested, bool debug = true) where S : struct
    {
        if (!EqualityComparer<S>.Default.Equals(nested, default))
        {
            return nested;
        }
        else if (!EqualityComparer<S>.Default.Equals(value, default))
        {
            return value;
        }

        if (debug)
        {
            LogWarning($"Both legacy-nested and field {nameof(value)} are default {typeof(S)}");
        }

        return default!;
    }

    public string ResolveFieldsFallback(string value, string nested, bool debug = true)
    {
        if (!string.IsNullOrEmpty(nested))
        {
            return nested;
        }
        else if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (debug)
        {
            LogWarning($"Both legacy-nested and field {nameof(value)} are null or empty strings");
        }

        return string.Empty;
    }

    public Vector3 GetPositionFallback(Vector3 position, string weObjectFilter, bool debug = true)
    {
        if (position != Vector3.zero)
        {
            return position;
        }
        else if (WorldEventUtils.TryGetRandomWorldEventObjectFromFilter(weObjectFilter, (uint)Builder.SessionSeedRandom.Seed, out var weObject) && weObject.enabled)
        {
            return weObject.gameObject.transform.position;
        }

        if (debug)
        {
            LogWarning($"Position is zero, or could not find enabled WorldEventObjectFilter {weObjectFilter}");
        }

        return Vector3.zero;
    }

    public static void ExecuteWardenEvents(List<WardenObjectiveEventData> events) 
        => WOManager.CheckAndExecuteEventsOnTrigger(events.ToIl2Cpp(), eWardenObjectiveEventTrigger.None, ignoreTrigger: true);
}
