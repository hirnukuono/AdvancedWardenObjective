using AIGraph;
using AmorLib.Utils;
using AmorLib.Utils.Extensions;
using BepInEx.Logging;
using GameData;
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

        var tp = e.TeleportPlayer ?? new();
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

        bool overflow = tp.FullTeamOverflow && tp.TPData.Count == 4 && tp.TPData.Max(tpd => (int)tpd.PlayerIndex) < 4;
        var itemAssignment = AssignWarpables(tp, overflow, playersInLevel);
        int usedCount = 0;
        for (int j = 0; j < playersInLevel.Count; j++)
        {
            PlayerAgent player = playersInLevel[j];
            if (!PlayerIsInLocation(player, tp.FromLocation))
            {
                if (itemAssignment.TryGetValue(j, out var items))
                    WarpItemsTo(player, items);
                continue;
            }

            int p = overflow ? (usedCount % 4) : usedCount;
            usedCount++;
            int idx = tp.TPData.FindIndex(tpd => (int)tpd.PlayerIndex == p);
            if (idx == -1) continue;
            var playerData = tp.TPData[idx];

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

    private static Dictionary<int, List<IWarpableObject>> AssignWarpables(WEE_TeleportPlayer tp, bool overflow, Il2CppPlayerList lobby)
    {
        var itemAssignment = new Dictionary<int, List<IWarpableObject>>();

        foreach (var item in Dimension.WarpableObjects)
        {
            var sentry = item.TryCast<SentryGunInstance>();
            if (sentry != null && tp.WarpSentries && NodeIsInLocation(sentry.CourseNode, tp.FromLocation))
            {
                itemAssignment.GetOrAddNew(lobby.IndexOf(sentry.Owner)).Add(item);
                continue;
            }

            var bigPickup = item.TryCast<ItemInLevel>();
            if (!tp.FlashTeleport && tp.WarpBigPickups && (overflow || lobby.Count == tp.TPData.Count || tp.FromLocation.Enabled)
                && bigPickup != null && bigPickup.CanWarp && NodeIsInLocation(bigPickup.CourseNode, tp.FromLocation) && bigPickup.internalSync.GetCurrentState().placement.droppedOnFloor)
            {
                itemAssignment.GetOrAddNew(tp.SendBPUsToHost ? PlayerManager.GetLocalPlayerAgent().PlayerSlotIndex : MasterRand.Next(lobby.Count)).Add(item);
            }
        }

        return itemAssignment;
    }

    static IEnumerator FlashBack(TeleportData tpData)
    {
        int reloadCount = CheckpointManager.CheckpointUsage;
        Logger.Verbose(LogLevel.Debug, $"{tpData.PlayerIndex} flash warp to {tpData.LastDimension} is queued...");

        yield return new WaitForSeconds(tpData.Duration);

        if (GameStateManager.CurrentStateName != eGameStateName.InLevel || reloadCount < CheckpointManager.CheckpointUsage)
        {
            // checkpoint was used or not in level, exit
            yield break; 
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

        WarpItemsTo(tpData.Player, tpData.Position, tpData.Dimension, tpData.ItemsToWarp);
    }

    private static void WarpItemsTo(PlayerAgent player, List<IWarpableObject> items) => WarpItemsTo(player, player.Position, player.DimensionIndex, items);
    private static void WarpItemsTo(PlayerAgent player, Vector3 position, eDimensionIndex dimension, List<IWarpableObject> items)
    {
        foreach (var item in items)
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
            if (bigPickup != null && bigPickup.GetSyncComponent().GetCurrentState().status != ePickupItemStatus.PickedUp)
            {
                bigPickup.GetSyncComponent().AttemptPickupInteraction
                (
                    ePickupItemInteractionType.Place,
                    null,
                    bigPickup.pItemData.custom,
                    position,
                    Quaternion.identity,
                    CourseNodeUtil.GetCourseNode(position, dimension),
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

    private static Vector3 CamDirIfNotBot(PlayerAgent player)
    {
        if (player.Owner.IsBot)
            return Vector3.forward;

        if (player.IsLocallyOwned)
            return player.FPSCamera.CameraRayDir.normalized;

        return player.Sync.m_locomotionData.LookDir.Value;
    }

    private static bool PlayerIsInLocation(PlayerAgent player, WEE_TeleportPlayer.FromLocationData data)
    {
        if (!data.Enabled) return true;

        var node = player.CourseNode;
        if (node == null)
        {
            Logger.Verbose(LogLevel.Warning, $"Player {player.Owner.NickName} has no CourseNode, can't check FromLocation filter!");
            return false;
        }

        return NodeIsInLocation(node, data);
    }

    private static bool NodeIsInLocation(AIG_CourseNode node, WEE_TeleportPlayer.FromLocationData data)
    {
        if (!data.Enabled) return true;

        if (node == null) return false;

        (var nodeDim, var nodeLayer, var nodeZone) = (node.m_dimension.DimensionIndex, node.LayerType, node.m_zone?.LocalIndex ?? eLocalZoneIndex.Zone_0);
        foreach (var d in data.DimensionIndex.Values)
        {
            foreach (var l in data.Layer.Values)
            {
                foreach (var z in data.LocalIndex.Values)
                {
                    if (nodeDim == d && nodeLayer == l && nodeZone == z)
                        return true;
                }
            }
        }
        return false;
    }
}