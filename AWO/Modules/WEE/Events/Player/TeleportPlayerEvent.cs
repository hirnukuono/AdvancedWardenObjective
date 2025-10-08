using AIGraph;
using BepInEx.Logging;
using LevelGeneration;
using Player;
using System.Collections;
using UnityEngine;
using Il2CppPlayerList = Il2CppSystem.Collections.Generic.List<Player.PlayerAgent>;
using TeleportData = AWO.Modules.WEE.WEE_TeleportPlayer.TeleportData;

namespace AWO.Modules.WEE.Events;

internal sealed class TeleportPlayerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.TeleportPlayer;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
        {
            LogError("Not in level!!!");
            return;
        }

        var tp = e.TeleportPlayer;
        var playersInLevel = PlayerManager.PlayerAgentsInLevel;

        if (tp.TPData.Count == 0) // convert old to new format
        {
            LogWarning($"No TPData provided, we will convert! {Name} has been changed (see AWO wiki)");
            var activeSlotIndices = new HashSet<int>(tp.PlayerFilter.Select(filter => (int)filter));

            for (int i = 0; i < playersInLevel.Count; i++)
            {
                if (!activeSlotIndices.Contains(i)) continue;                
                var (pos, dir) = i switch
                {
                    0 => (tp.Player0Position, tp.P0LookDir),
                    1 => (tp.Player1Position, tp.P1LookDir),
                    2 => (tp.Player2Position, tp.P2LookDir),
                    3 => (tp.Player3Position, tp.P3LookDir),
                    _ => (playersInLevel[i].Position, 4)
                };
                tp.TPData.Add(new()
                {
                    PlayerIndex = (PlayerIndex)i,
                    Position = pos,
                    LookDir = dir,
                    PlayWarpAnimation = tp.PlayWarpAnimation
                });
            }
        }

        var itemAssignment = AssignWarpables(tp, playersInLevel);

        for (int j = 0; j < playersInLevel.Count; j++)
        {
            bool overflow = j >= 4 && tp.FullTeamOverflow && tp.TPData.Max(tpd => (int)tpd.PlayerIndex) < 4;
            int p = overflow ? (j % 4) : j;
            int idx = tp.TPData.FindIndex(tpd => (int)tpd.PlayerIndex == p);
            if (idx == -1) continue;
            var playerData = tp.TPData[idx];

            PlayerAgent player = playersInLevel[j];
            var tpData = new TeleportData()
            {
                Player = player,
                PlayerIndex = playerData.PlayerIndex,
                Dimension = ResolveFieldsFallback(e.DimensionIndex, playerData.Dimension, false),
                Position = GetPositionFallback(ResolveFieldsFallback(e.Position, playerData.Position, false), ResolveFieldsFallback(e.SpecialText, playerData.WorldEventObjectFilter, false)),
                LookDirV3 = ResolveFieldsFallback(GetLookDirV3(player, playerData.LookDir), playerData.LookDirV3, false),
                PlayWarpAnimation = tp.PlayWarpAnimation || playerData.PlayWarpAnimation,
                Duration = ResolveFieldsFallback(e.Duration, playerData.Duration, tp.FlashTeleport),
                LastDimension = player.DimensionIndex,
                LastPosition = player.Position,
                LastLookDirV3 = CamDirIfNotBot(player),
                ItemsToWarp = itemAssignment.GetOrAddNew(j)
            };
            if (tp.FlashTeleport)
            {
                CoroutineManager.StartCoroutine(FlashBack(tpData).WrapToIl2Cpp());
            }
            DoTeleport(tpData);
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
            if (!tp.FlashTeleport && tp.WarpBigPickups && lobby.Count == tp.TPData.Count
                && bigPickup != null && bigPickup.CanWarp && bigPickup.internalSync.GetCurrentState().placement.droppedOnFloor)
            {
                itemAssignment.GetOrAddNew(tp.SendBPUsToHost ? PlayerManager.GetLocalPlayerAgent().PlayerSlotIndex : MasterRand.Next(lobby.Count)).Add(item);
            }
        }

        return itemAssignment;
    }

    static IEnumerator FlashBack(TeleportData tpData)
    {
        int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        Logger.Verbose(LogLevel.Debug, $"{tpData.PlayerIndex} flash warp to {tpData.LastDimension} is queued...");

        yield return new WaitForSeconds(tpData.Duration);

        if (GameStateManager.CurrentStateName != eGameStateName.InLevel || CheckpointManager.Current.m_stateReplicator.State.reloadCount > reloadCount)
        {
            yield break; // checkpoint was used or not in level, exit
        }

        Logger.Verbose(LogLevel.Debug, $"Warping {tpData.PlayerIndex} back to {tpData.LastDimension}");

        tpData.Dimension = tpData.LastDimension;
        tpData.Position = tpData.LastPosition;
        tpData.LookDirV3 = tpData.LookDir != 4 ? tpData.LastLookDirV3 : CamDirIfNotBot(tpData.Player);

        DoTeleport(tpData);
    }

    private static void DoTeleport(TeleportData tpData)
    {
        if (tpData.Player.Owner.IsBot)
        {
            tpData.Player.TryWarpTo(tpData.Dimension, tpData.Position, Vector3.forward);
        }
        else
        {
            tpData.Player.Sync.SendSyncWarp(tpData.Dimension, tpData.Position, tpData.LookDirV3, tpData.PlayWarpAnimation ? PlayerAgent.WarpOptions.All : PlayerAgent.WarpOptions.PlaySounds);
        }

        foreach (var item in tpData.ItemsToWarp)
        {
            var sentry = item.TryCast<SentryGunInstance>();
            if (sentry != null)
            {
                if (sentry.LocallyPlaced)
                {
                    sentry.m_sync.WantItemAction(tpData.Player, SyncedItemAction_New.PickUp);
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
                    tpData.Position,
                    Quaternion.identity,
                    GetNodeFromDimPos(tpData.Dimension, tpData.Position)?.CourseNode,
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
            4 => CamDirIfNotBot(player),
            _ => Vector3.forward,
        };
    }

    private static Vector3 CamDirIfNotBot(PlayerAgent player) => player.Owner.IsBot ? Vector3.forward : player.Sync.m_locomotionData.LookDir.Value;

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