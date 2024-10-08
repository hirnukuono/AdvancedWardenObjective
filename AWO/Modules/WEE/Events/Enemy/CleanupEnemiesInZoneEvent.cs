﻿using AWO.Modules.WEE;
using LevelGeneration;

namespace AWO.WEE.Events.Enemy;

internal class CleanupEnemiesInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.CleanupEnemiesInZone;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone))
        {
            LogError("Zone is Missing?");
            return;
        }

        var data = e.CleanupEnemies;
        if (data == null)
        {
            LogError("CleanupEnemies Data is null?");
            return;
        }

        if (data.AreaIndex == -1)
        {
            foreach (var node in zone.m_courseNodes)
            {
                data.DoClear(node);
            }
        }
        else if (IsValidAreaIndex(data.AreaIndex, zone))
        {
            data.DoClear(zone.m_areas[data.AreaIndex].m_courseNode);
        }
    }

    private static bool IsValidAreaIndex(int areaIndex, LG_Zone zone)
    {
        var areas = zone.m_areas;
        if (areaIndex < 0 || areaIndex >= areas.Count)
        {
            Logger.Error($"[CleanupEnemiesInZoneEvent] Invalid area index {areaIndex}");
            return false;
        }

        return true;
    }
}
