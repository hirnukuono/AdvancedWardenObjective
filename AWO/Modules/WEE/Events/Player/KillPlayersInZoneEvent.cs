using Player;

namespace AWO.Modules.WEE.Events;
internal sealed class KillPlayersInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.KillPlayersInZone;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) return;

        int id = zone.ID;
        foreach (var player in PlayerManager.PlayerAgentsInLevel)
        {
            if (player.CourseNode.m_zone.ID == id) 
                player.Damage.ExplosionDamage(player.Damage.DamageMax, default, default);
        }
    }
}
