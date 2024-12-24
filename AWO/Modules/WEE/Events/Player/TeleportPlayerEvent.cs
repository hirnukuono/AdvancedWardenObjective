﻿using AIGraph;
using LevelGeneration;
using Player;
using SNetwork;
using System.Collections;
using UnityEngine;
using Il2CppPlayerList = Il2CppSystem.Collections.Generic.List<Player.PlayerAgent>;

namespace AWO.Modules.WEE.Events;

internal sealed class TeleportPlayerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.TeleportPlayer;

    public class TPData
    {
        public PlayerIndex PlayerIndex { get; set; }
        public Vector3 Position { get; set; }
        public int LookDir { get; set; }
        public eDimensionIndex LastDim { get; set; }
        public Vector3 LastPosition { get; set; }
        public Vector3 LastLookDir { get; set; }
        public List<IWarpableObject> ItemsToWarp { get; set; }

        public TPData(PlayerIndex a, Vector3 b, int c, eDimensionIndex d, Vector3 e, Vector3 f, List<IWarpableObject> g)
        {
            PlayerIndex = a;
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
        var itemAssignment = AssignWarpables(e.TeleportPlayer, PlayerManager.PlayerAgentsInLevel);
        var slotPlayerData = new Dictionary<int, TPData>
        {
            { 0, new TPData(PlayerIndex.P0, e.TeleportPlayer.Player0Position, e.TeleportPlayer.P0LookDir, 0, Vector3.zero, Vector3.zero, itemAssignment[0]) },
            { 1, new TPData(PlayerIndex.P1, e.TeleportPlayer.Player1Position, e.TeleportPlayer.P1LookDir, 0, Vector3.zero, Vector3.zero, itemAssignment[1]) },
            { 2, new TPData(PlayerIndex.P2, e.TeleportPlayer.Player2Position, e.TeleportPlayer.P2LookDir, 0, Vector3.zero, Vector3.zero, itemAssignment[2]) },
            { 3, new TPData(PlayerIndex.P3, e.TeleportPlayer.Player3Position, e.TeleportPlayer.P3LookDir, 0, Vector3.zero, Vector3.zero, itemAssignment[3]) }
        };
        var activeSlotIndices = new HashSet<int>(e.TeleportPlayer.PlayerFilter.Select(filter => (int)filter));

        for (int i = 0; i < PlayerManager.PlayerAgentsInLevel.Count; i++)
        {
            PlayerAgent player = PlayerManager.PlayerAgentsInLevel[i];
            if (!activeSlotIndices.Contains(i))
                continue; // Player not in PlayerFilter
            if (!slotPlayerData.TryGetValue(i, out var playerData))
                continue; // Player not in SlotIndex

            if (e.TeleportPlayer.FlashTeleport)
            {        
                EntryPoint.Coroutines.TPFStarted = Time.realtimeSinceStartup;
                playerData.LastDim = player.DimensionIndex;
                playerData.LastPosition = player.Position;
                playerData.LastLookDir = !player.Owner.IsBot ? player.FPSCamera.CameraRayDir : Vector3.forward;
                CoroutineManager.StartCoroutine(FlashBack(e, player, playerData).WrapToIl2Cpp());
            }

            Teleport(e, player, playerData);
        }
    }

    private static Dictionary<int, List<IWarpableObject>> AssignWarpables(WEE_TeleportPlayer tp, Il2CppPlayerList lobby)
    {
        var itemAssignment = new Dictionary<int, List<IWarpableObject>>();

        foreach (var item in Dimension.WarpableObjects)
        {
            var sentry = item.TryCast<SentryGunInstance>();
            if (sentry != null && tp.WarpSentries)
            {
                itemAssignment.GetOrAddNew(lobby.IndexOf(sentry.Owner)).Add(item);
                continue;
            }

            var bigPickup = item.TryCast<ItemInLevel>();
            if (bigPickup == null || tp.FlashTeleport || !tp.WarpBigPickups)
                continue; 
            if (!bigPickup.internalSync.GetCurrentState().placement.droppedOnFloor || !bigPickup.CanWarp)
                continue; // warp only BPUs on the floor, otherwise continue

            if (HasMaster && tp.SendBPUsToHost)
                itemAssignment.GetOrAddNew(SNet.Master.PlayerSlotIndex()).Add(item);
            else if (lobby.Count == tp.PlayerFilter.Count)
                itemAssignment.GetOrAddNew(MasterRand.Next(lobby.Count)).Add(item);
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

        Logger.Debug("[TeleportPlayerEvent] Warping players back...");
        Teleport(e, player, playerData);
    }

    private static void Teleport(WEE_EventData e, PlayerAgent player, TPData playerData)
    {
        Vector3 lookDirV3 = playerData.LastLookDir == Vector3.zero ? GetLookDirV3(player, playerData.LookDir) : playerData.LastLookDir;

        if (player.Owner.IsBot)
            player.TryWarpTo(e.DimensionIndex, playerData.Position, Vector3.forward);
        else if (e.TeleportPlayer.PlayWarpAnimation)
            player.Sync.SendSyncWarp(e.DimensionIndex, playerData.Position, lookDirV3, PlayerAgent.WarpOptions.All);
        else
            player.Sync.SendSyncWarp(e.DimensionIndex, playerData.Position, lookDirV3, PlayerAgent.WarpOptions.PlaySounds);

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