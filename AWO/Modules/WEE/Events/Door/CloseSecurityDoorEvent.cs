using AIGraph;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class CloseSecurityDoorEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.CloseSecurityDoor;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) return;

        var state = door.m_sync.GetCurrentSyncState();
        if (state.status != eDoorStatus.Open && state.status != eDoorStatus.Opening)
        {
            LogError("Door is already closed!");
            return;
        }

        var sync = door.m_sync.TryCast<LG_Door_Sync>();
        if (sync == null) return;

        LogDebug("Door closing...");
        state.status = eDoorStatus.Closed;
        state.hasBeenOpenedDuringGame = false;
        sync.m_stateReplicator.State = state;        

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
        if (sourceNode?.m_portals == null) return;

        foreach (var enemy in sourceNode.m_enemiesInNode.ToArray())
        {
            enemy.m_replicator.Despawn();
        }

        foreach (var portal in sourceNode.m_portals)
        {
            if (portal == null || portal.m_searchID == searchID) continue;

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
