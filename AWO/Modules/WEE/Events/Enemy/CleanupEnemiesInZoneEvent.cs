namespace AWO.Modules.WEE.Events;

internal class CleanupEnemiesInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.CleanupEnemiesInZone;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) return;

        foreach (var ce in e.CleanupEnemies.Values)
        {
            if (ce.AreaIndex == -1)
            {
                foreach (var node in zone.m_courseNodes)
                {
                    if (ce.AreaBlacklist.Contains(zone.m_courseNodes.IndexOf(node)))
                        continue;
                    ce.DoClear(node);
                }
            }
            else if (IsValidAreaIndex(ce.AreaIndex, zone))
            {
                ce.DoClear(zone.m_areas[ce.AreaIndex].m_courseNode);
            }
        }
    }
}
