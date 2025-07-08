using AIGraph;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class CloseSecurityDoorEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.CloseSecurityDoor;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) return;

        var state = door.m_sync.GetCurrentSyncState();
        if (state.status != eDoorStatus.Open && state.status != eDoorStatus.Opening)
        {
            LogError("Door is already closed!");
            return;
        }

        LogDebug("Door closing...");
        state.status = eDoorStatus.Closed;
        state.hasBeenOpenedDuringGame = false;
        door.m_sync.SetStateUnsynced(state);

        var gate = door.Gate;
        gate.HasBeenOpenedDuringPlay = false;
        gate.IsTraversable = false;

        if (e.CleanUpEnemiesBehind)
        {
            var nodeDistanceFrom = gate.m_linksFrom.m_courseNode.m_playerCoverage.GetNodeDistanceToPlayer();
            var nodeDistanceBehind = gate.m_linksTo.m_courseNode.m_playerCoverage.GetNodeDistanceToPlayer();
            var clearNode = nodeDistanceFrom < nodeDistanceBehind ? gate.m_linksTo.m_courseNode : gate.m_linksFrom.m_courseNode;
            LogDebug("Despawning enemies behind security door...");
            AIG_SearchID.IncrementSearchID();
            DespawnEnemiesInNearNodes(AIG_SearchID.SearchID, clearNode);
        }
    }

    private static void DespawnEnemiesInNearNodes(ushort searchID, AIG_CourseNode sourceNode)
    {
        if (sourceNode == null) return;

        if (sourceNode.m_portals == null) return;

        foreach (var enemy in sourceNode.m_enemiesInNode.ToArray())
        {
            enemy.m_replicator.Despawn();
        }

        foreach (var portal in sourceNode.m_portals)
        {
            if (portal == null) continue;

            if (portal.m_searchID == searchID) continue;

            portal.m_searchID = searchID;
            if (portal.IsProgressionLocked) continue;

            var behindNode = portal.GetOppositeNode(sourceNode);
            if (behindNode != null)
            {
                DespawnEnemiesInNearNodes(searchID, behindNode);
            }
        }
    }
}
