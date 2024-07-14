using AWO.WEE.Events;
using Player;

namespace AWO.Modules.WEE.Events.World;
internal sealed class KillPlayersInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.KillPlayersInZone;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone))
        {
            LogError("Cannot find zone!");
            return;
        }

        var id = zone.ID;
        foreach (var agent in PlayerManager.PlayerAgentsInLevel)
        {
            var node = agent.CourseNode;
            if (node == null)
                continue;

            if (node.m_zone == null)
                continue;
            
            if (node.m_zone.ID == id)
                agent.Damage.ExplosionDamage(agent.Damage.DamageMax, default, default);
        }
    }
}
