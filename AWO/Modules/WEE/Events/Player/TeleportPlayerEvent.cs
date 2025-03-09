using AIGraph;
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

    private static Vector3 CamDirIfNotBot(PlayerAgent player) => !player.Owner.IsBot ? player.FPSCamera.CameraRayDir : Vector3.forward;

    protected override void TriggerMaster(WEE_EventData e)
    {
        var tp = e.TeleportPlayer;
        var playersInLevel = PlayerManager.PlayerAgentsInLevel;
        var itemAssignment = AssignWarpables(tp, playersInLevel);
        var activeSlotIndices = new HashSet<int>(tp.PlayerFilter.Select(filter => (int)filter));

        for (int i = 0; i < playersInLevel.Count; i++)
        {
            if (!activeSlotIndices.Contains(i)) continue; // Player not in PlayerFilter            
            PlayerAgent player = playersInLevel[i];

            var (pos, dir) = i switch
            {
                0 => (tp.Player0Position, tp.P0LookDir),
                1 => (tp.Player1Position, tp.P1LookDir),
                2 => (tp.Player2Position, tp.P2LookDir),
                3 => (tp.Player3Position, tp.P3LookDir),
                _ => (player.Position, 4)
            };
            var playerData = new TPData((PlayerIndex)i, pos, dir, player.DimensionIndex, player.Position, Vector3.zero, itemAssignment.GetOrAddNew(i));

            if (tp.FlashTeleport)
            {        
                EntryPoint.Coroutines.TPFStarted = Time.realtimeSinceStartup;
                playerData.LastLookDir = CamDirIfNotBot(player);
                CoroutineManager.StartCoroutine(FlashBack(e.Duration, player, playerData, tp.PlayWarpAnimation).WrapToIl2Cpp());
            }

            DoTeleport(e.DimensionIndex, player, playerData, tp.PlayWarpAnimation);
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
            if (tp.FlashTeleport || !tp.WarpBigPickups || bigPickup == null || !bigPickup.CanWarp || !bigPickup.internalSync.GetCurrentState().placement.droppedOnFloor)
            {
                continue; // warp only BPUs on the floor, otherwise continue
            }

            if (HasMaster && tp.SendBPUsToHost)
            {
                itemAssignment.GetOrAddNew(PlayerManager.GetLocalPlayerAgent().PlayerSlotIndex).Add(item);
            }
            else if (lobby.Count == tp.PlayerFilter.Count)
            {
                itemAssignment.GetOrAddNew(MasterRand.Next(lobby.Count)).Add(item);
            }
        }

        return itemAssignment;
    }

    static IEnumerator FlashBack(float duration, PlayerAgent player, TPData playerData, bool playAnim)
    {
        int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float startTime = EntryPoint.Coroutines.TPFStarted;
        playerData.Position = playerData.LastPosition;

        yield return new WaitForSeconds(duration);

        if (GameStateManager.CurrentStateName != eGameStateName.InLevel || CheckpointManager.Current.m_stateReplicator.State.reloadCount > reloadCount || startTime < EntryPoint.Coroutines.TPFStarted)
        {
            yield break; // checkpoint was used or not in level or canceled by another warp, exit
        }

        Logger.Debug("[TeleportPlayerEvent] Warping players back...");
        DoTeleport(playerData.LastDim, player, playerData, playAnim);
    }

    private static void DoTeleport(eDimensionIndex dim, PlayerAgent player, TPData playerData, bool playAnim)
    {
        Vector3 lookDirV3 = playerData.LastLookDir == Vector3.zero ? GetLookDirV3(player, playerData.LookDir) : playerData.LastLookDir;

        if (player.Owner.IsBot)
        {
            player.TryWarpTo(dim, playerData.Position, Vector3.forward);
        }
        else
        {
            player.Sync.SendSyncWarp(dim, playerData.Position, lookDirV3, playAnim ? PlayerAgent.WarpOptions.All : PlayerAgent.WarpOptions.PlaySounds);
        }

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
                    GetNodeFromDimPos(dim, playerData.Position)?.CourseNode,
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