namespace AWO.Modules.WEE.Events;

internal sealed class CleanupEnemiesInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.CleanupEnemiesInZone;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) 
            return;

        foreach (var ce in e.CleanupEnemies.Values)
        {
            if (ce.AreaIndex == -1)
            {
                foreach (var area in zone.m_areas)
                {
                    if (ce.AreaBlacklist.Contains(zone.m_areas.IndexOf(area))) 
                        continue;
                    ce.DoClear(area.m_courseNode);
                }
            }
            else if (IsValidAreaIndex(ce.AreaIndex, zone))
            {
                ce.DoClear(zone.m_areas[ce.AreaIndex].m_courseNode);
            }
        }
    }
}
