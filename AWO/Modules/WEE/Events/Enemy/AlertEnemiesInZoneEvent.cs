using Agents;
using AIGraph;
using BepInEx.Logging;
using Player;
using System.Diagnostics.CodeAnalysis;

namespace AWO.Modules.WEE.Events;

internal sealed class AlertEnemiesInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.AlertEnemiesInZone;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) return;

        if (e.SpecialNumber == -1)
        {
            foreach (var node in zone.m_courseNodes)
            {
                DoAlert(node);
            }
        }
        else if (IsValidAreaIndex(e.SpecialNumber, zone))
        {
            DoAlert(zone.m_areas[e.SpecialNumber].m_courseNode);
        }
    }

    private static void DoAlert(AIG_CourseNode node)
    {
        if (node.m_enemiesInNode == null || node.listenersInNode == null)
        {
            Logger.Error("AlertEnemiesInZoneEvent", "Null enemies or listeners in node!");
            return;
        }

        NoiseManager.MakeNoise(new()
        {
            noiseMaker = null,
            node = node,
            position = node.GetRandomPositionInside(),
            radiusMin = 0.0f,
            radiusMax = 64.0f,
            yScale = 1.0f,
            type = NM_NoiseType.InstaDetect,
            includeToNeightbourAreas = false, // 10cc typo literaly unplayable
            raycastFirstNode = false
        });

        if (TryGetClosestAlivePlayerByNode(node, out var minae))
        {
            foreach (var enemy in node.m_enemiesInNode)
            {
                AgentMode mode = AgentMode.Agressive;
                enemy.AI.SetStartMode(mode);
                enemy.AI.ModeChange();
                enemy.AI.m_mode = mode;
                enemy.AI.SetDetectedAgent(minae, AgentTargetDetectionType.DamageDetection);
            }
        }
        else
        {
            Logger.Warn("AlertEnemiesInZoneEvent", "Failed to find closest alive player target!");
        }
    }

    public static bool TryGetClosestAlivePlayerByNode(AIG_CourseNode node, [NotNullWhen(true)] out PlayerAgent? player)
    {
        PlayerAgent? humanPlayer = null;        
        PlayerAgent? botPlayer = null;
        int humanDist = int.MaxValue;
        int botDist = int.MaxValue;        
        var coverageDatas = node.m_playerCoverage.m_coverageDatas;

        int count = Math.Min(PlayerManager.PlayerAgentsInLevel.Count, coverageDatas.Length);
        for (int i = 0; i < count; i++)
        {
            var agent = PlayerManager.PlayerAgentsInLevel[i];
            var coverage = coverageDatas[i];
            if (agent == null || !agent.Alive || coverage == null || !coverage.IsValidNodeDistance)
            {
                continue;
            }

            int nodeDist = coverage.m_nodeDistance;
            if (agent.Owner.IsBot)
            {
                if (nodeDist < botDist)
                {
                    botDist = nodeDist;
                    botPlayer = agent;
                }
            }
            else
            {
                if (nodeDist < humanDist)
                {
                    humanDist = nodeDist;
                    humanPlayer = agent;
                }
            }
        }

        player = humanPlayer ?? botPlayer;
        Logger.Verbose(LogLevel.Debug, $"Closest alive player target to {node.m_zone.LocalIndex}, {node.m_zone.m_courseNodes.IndexOf(node)}: {player?.PlayerName ?? "null"})");
        return player != null;
    }
}