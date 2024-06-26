using AWO.WEE.Events;
using AIGraph;
using LevelGeneration;
using Player;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events.World;

internal sealed class TeleportPlayerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.TeleportPlayer;

    private class TPData
    {
        public SlotIndex SlotIndex { get; set; }
        public Vector3 Position { get; set; }
        public int LookDir { get; set; }
        public eDimensionIndex LastDim { get; set; }
        public Vector3 LastPosition { get; set; }
        public Vector3 LastLookDir { get; set; }
        public List<IWarpableObject> ItemsToWarp { get; set; }

        public TPData(SlotIndex a, Vector3 b, int c, eDimensionIndex d, Vector3 e, Vector3 f, List<IWarpableObject> g)
        {
            SlotIndex = a;
            Position = b;
            LookDir = c;
            LastDim = d;
            LastPosition = e;
            LastLookDir = f;
            ItemsToWarp = g;
        }
    }

    protected override void TriggerMaster(WEE_EventData e)
    {
        var itemAssignment = AssignWarpables(e, PlayerManager.PlayerAgentsInLevel);
        var slotPlayerData = new Dictionary<int, TPData>
        {
            { 0, new TPData(SlotIndex.P0, e.TeleportPlayer.Player0Position, e.TeleportPlayer.P0LookDir, 0, Vector3.zero, Vector3.zero, itemAssignment[0]) },
            { 1, new TPData(SlotIndex.P1, e.TeleportPlayer.Player1Position, e.TeleportPlayer.P1LookDir, 0, Vector3.zero, Vector3.zero, itemAssignment[1]) },
            { 2, new TPData(SlotIndex.P2, e.TeleportPlayer.Player2Position, e.TeleportPlayer.P2LookDir, 0, Vector3.zero, Vector3.zero, itemAssignment[2]) },
            { 3, new TPData(SlotIndex.P3, e.TeleportPlayer.Player3Position, e.TeleportPlayer.P3LookDir, 0, Vector3.zero, Vector3.zero, itemAssignment[3]) }
        };
        var activeSlotIndices = new HashSet<int>(e.TeleportPlayer.PlayerFilter.Select(filter => (int)filter));
        EntryPoint.Coroutines.TPFStarted = Time.realtimeSinceStartup;

        foreach (PlayerAgent player in PlayerManager.PlayerAgentsInLevel)
        {
            if (!activeSlotIndices.Contains(player.PlayerSlotIndex))
                continue; // Player not in PlayerFilter, continue
            if (!slotPlayerData.TryGetValue(player.PlayerSlotIndex, out var playerData))
                continue; // Player has no TPData, continue

            if (e.TeleportPlayer.FlashTeleport)
            {
                playerData.LastDim = player.DimensionIndex;
                playerData.LastPosition = player.Position;
                playerData.LastLookDir = !player.Owner.IsBot ? player.FPSCamera.CameraRayDir : Vector3.forward;
                CoroutineManager.StartCoroutine(FlashBack(e, player, playerData).WrapToIl2Cpp());
            }

            Teleport(e, player, playerData);
        }
    }

    private static Dictionary<int, List<IWarpableObject>> AssignWarpables(WEE_EventData e, Il2CppSystem.Collections.Generic.List<PlayerAgent> lobby)
    {
        var itemAssignment = new Dictionary<int, List<IWarpableObject>>();
        var warpables = Dimension.WarpableObjects;

        for (int i = 0; i < 4; i++)
            itemAssignment[i] = new List<IWarpableObject>();

        foreach (var item in warpables)
        {
            var sentry = item.TryCast<SentryGunInstance>();
            if (sentry != null && e.TeleportPlayer.WarpSentries)
            {
                itemAssignment[lobby.IndexOf(sentry.Owner)].Add(item);
                continue;
            }

            var bigPickup = item.TryCast<ItemInLevel>();
            if (bigPickup == null || e.TeleportPlayer.FlashTeleport || !e.TeleportPlayer.WarpBigPickups)
                continue; 
            if (!bigPickup.internalSync.GetCurrentState().placement.droppedOnFloor || !bigPickup.CanWarp)
                continue; // warp only BPUs on the floor, otherwise continue

            if (e.TeleportPlayer.SendBPUsToHost)
                itemAssignment[0].Add(item);
            else if (lobby.Count == e.TeleportPlayer.PlayerFilter.Count)
                itemAssignment[RNG.Int0Positive % lobby.Count].Add(item);
        }

        return itemAssignment;
    }

    static IEnumerator FlashBack(WEE_EventData e, PlayerAgent player, TPData playerData)
    {
        int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float startTime = EntryPoint.Coroutines.TPFStarted;
        e.DimensionIndex = playerData.LastDim;
        playerData.Position = playerData.LastPosition;

        yield return new WaitForSeconds(e.Duration);

        if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
            yield break; // no longer in level, exit
        if (startTime < EntryPoint.Coroutines.TPFStarted)
            yield break; // new TeleportPlayer event started, exit
        if (CheckpointManager.Current.m_stateReplicator.State.reloadCount > reloadCount)
            yield break; // checkpoint was used, exit

        Logger.Debug($"AdvancedWardenObjective - Teleporting players back...");
        Teleport(e, player, playerData);
    }

    private static void Teleport(WEE_EventData e, PlayerAgent player, TPData playerData)
    {
        if (e.TeleportPlayer.PlayWarpAnimation || player.Owner.IsBot)
            player.TryWarpTo(e.DimensionIndex, playerData.Position, playerData.LastLookDir == Vector3.zero ? GetLookDirV3(player, playerData.LookDir) : default);
        else
            player.TeleportTo(playerData.Position);

        foreach (var item in playerData.ItemsToWarp)
        {
            var sentry = item.TryCast<SentryGunInstance>();
            if (sentry != null)
            {
                if (sentry.LocallyPlaced)
                {
                    sentry.m_sync.WantItemAction(player, SyncedItemAction_New.PickUp);
                    continue;
                }
            }

            var bigPickup = item.TryCast<ItemInLevel>();
            if ((bigPickup?.GetSyncComponent().GetCurrentState().status) != ePickupItemStatus.PickedUp)
            {
                bigPickup?.GetSyncComponent().AttemptPickupInteraction
                (
                    ePickupItemInteractionType.Place,
                    null,
                    bigPickup.pItemData.custom,
                    playerData.Position,
                    Quaternion.identity,
                    GetNodeFromDimPos(e.DimensionIndex, playerData.Position)?.CourseNode,
                    true,
                    true
                );
            }
        }
    }

    private static Vector3 GetLookDirV3(PlayerAgent player, int lookDir)
    {
        return lookDir switch
        {
            0 => Vector3.forward,
            1 => Vector3.left,
            2 => Vector3.right,
            3 => Vector3.back,
            4 => !player.Owner.IsBot ? player.FPSCamera.CameraRayDir : Vector3.forward,
            _ => Vector3.forward,
        };
    }

    private static AIG_NodeCluster? GetNodeFromDimPos(eDimensionIndex dimensionIndex, Vector3 position)
    {
        if (!AIG_GeomorphNodeVolume.TryGetGeomorphVolume(0, dimensionIndex, position, out var resultingGeoVolume)
            || !resultingGeoVolume.m_voxelNodeVolume.TryGetPillar(position, out var pillar)
            || !pillar.TryGetVoxelNode(position.y, out var bestNode)
            || !AIG_NodeCluster.TryGetNodeCluster(bestNode.ClusterID, out var nodeCluster)
           )
        {
            return null;
        }

        return nodeCluster;
    }
}