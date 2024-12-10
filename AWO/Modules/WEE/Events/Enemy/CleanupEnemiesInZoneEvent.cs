using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal class CleanupEnemiesInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.CleanupEnemiesInZone;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) return;

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
}
