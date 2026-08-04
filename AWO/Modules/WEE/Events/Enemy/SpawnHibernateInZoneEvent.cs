using Agents;
using AIGraph;
using AmorLib.Events;
using AmorLib.Utils;
using AmorLib.Utils.Extensions;
using BepInEx.Logging;
using Enemies;
using GTFO.API;
using LevelGeneration;
using Player;
using System.Collections;
using UnityEngine;
using Il2Collection = Il2CppSystem.Collections.Generic;

namespace AWO.Modules.WEE.Events;

internal class SpawnHibernateInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SpawnHibernateInZone;
    public override bool AllowArrayableGlobalIndex => true;

    #region PLACEMENT
    class PlacementHolder
    {
        private readonly AIG_INode[] _nodes;
        private int _index;
        private bool _hasLooped;

        private int MoveIndex()
        {
            var index = _index;
            if (++_index == _nodes.Length)
            {
                _hasLooped = true;
                _index = 0;
            }
            return index;
        }

        public Vector3 GetNextPosition()
        {
            AIG_INode node;
            do
            {
                node = _nodes[MoveIndex()];
            }
            while (!_hasLooped && node.PlacementHeat > 999f);

            node.PlacementHeat = 1000f;
            return node.Position;
        }

        public void SortNodes()
        {
            Array.Sort(_nodes, (x, y) => x.PlacementHeat.CompareTo(y.PlacementHeat));
        }

        public void Reset()
        {
            _index = 0;
        }

        public PlacementHolder(AIG_INode[] nodes)
        {
            _nodes = nodes;
            _hasLooped = false;
            _index = 0;
        }
    }

    private const int MaxPlacementNodes = 100;
    private static PlacementHolder[] s_placementHolders = null!;

    private static float s_currentRandomWeight;
    private static readonly Queue<AIG_INode> s_nodesToSpreadFrom = new();
    private static Il2Collection.List<LG_Scoring.Score<LG_Area>> s_currentScoredAreas = null!;
    #endregion

    #region SPAWN_QUEUE
    struct QueuedSpawn
    {
        public uint EnemyID;
        public AIG_CourseNode Node;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool Enabled;
        public bool Random;

        public QueuedSpawn(uint enemyID, AIG_CourseNode node, Vector3 position, Quaternion rotation)
        {
            EnemyID = enemyID;
            Node = node;
            Position = position;
            Rotation = rotation;
            Enabled = true;
            Random = false;
        }

        public QueuedSpawn(uint enemyID, AIG_CourseNode node, bool enabled)
        {
            EnemyID = enemyID;
            Node = node;
            Position = Vector3.zero;
            Rotation = Quaternion.Euler(0, MasterRand.NextRange(0, 360), 0); ;
            Enabled = enabled;
            Random = true;
        }
    }

    private readonly static WaitForSeconds?[] s_spawnInterval = new WaitForSeconds?[] { null, new(0.05f), new(0.15f) };
    private readonly static Queue<QueuedSpawn>[] s_spawnQueues = new Queue<QueuedSpawn>[] { new (), new (), new () };
    private static Coroutine? s_updateRoutine;
    #endregion

    protected override void OnSetup()
    {
        base.OnSetup();

        LevelAPI.OnAfterBuildBatch += HandlePlacementBatches;
        LevelAPI.OnLevelCleanup += CleanupSpawnUpdate;
        SNetEvents.OnCheckpointReload += CleanupSpawnUpdate;
    }

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) 
            return;

        foreach (var sh in e.SpawnHibernates.Values)
        {
            if (sh.ResetPlacementInfo)
                ResetPlacements(zone);

            if (sh.AreaIndex == -1 || IsValidAreaIndex(sh.AreaIndex, zone))
            {
                Vector3 pos = GetPositionFallback(ResolveFieldsFallback(e.Position, sh.Position, false), e.SpecialText, false);
                int count = ResolveFieldsFallback(e.Count, sh.Count);

                if (count == 1 && pos != Vector3.zero) // spawn 1 enemy at a specific position
                {
                    var node = sh.AreaIndex == -1 ? CourseNodeUtil.GetCourseNode(pos) : zone.m_areas[sh.AreaIndex].m_courseNode;
                    QueueSpawn(sh.EnemyID, node, pos, Quaternion.Euler(sh.Rotation));
                }
                else
                {
                    QueueRandomSpawns(sh, zone, count, e.Enabled);
                }
            }
        }
    }

    private static void QueueRandomSpawns(WEE_SpawnHibernateData sh, LG_Zone zone, int count, bool enabled)
    {
        var areas = zone.m_areas;
        var validAreas = Enumerable.Range(0, areas.Count).Except(sh.AreaBlacklist).ToList();

        if (validAreas.Count == 0)
        {
            Logger.Error("SpawnHibernateInZoneEvent", $"No valid areas to spawn hibernate! Area count: {areas.Count}, Blacklist: [{string.Join(", ", sh.AreaBlacklist)}]");
            return;
        }

        bool useRandomArea = sh.AreaIndex == -1;
        AIG_CourseNode spawnNode;
        if (useRandomArea)
        {
            s_currentRandomWeight = sh.PlacementScoreRandomWeight;
            Il2Collection.List<LG_Area> il2Areas = new(validAreas.Count);
            foreach (var index in validAreas)
                il2Areas.Add(areas[index]);
            s_currentScoredAreas = LG_Scoring.CreateScores(il2Areas, 0f);
            spawnNode = null!;
        }
        else
        {
            spawnNode = areas[sh.AreaIndex].m_courseNode;
        }

        for (int spawnCount = 0; spawnCount < count; spawnCount++)
        {
            if (useRandomArea)
                spawnNode = GetRandomSpawnNode(sh);

            QueueSpawn(sh.EnemyID, spawnNode, enabled);
        }
    }

    private static void QueueSpawn(uint enemyID, AIG_CourseNode node, Vector3 position, Quaternion rotation)
    {
        int nodeDist = Math.Min(node.m_playerCoverage.GetNodeDistanceToClosestPlayer(), 2);
        s_spawnQueues[nodeDist].Enqueue(new(enemyID, node, position, rotation));
        s_updateRoutine ??= CoroutineManager.StartCoroutine(SpawnUpdate().WrapToIl2Cpp());
    }

    private static void QueueSpawn(uint enemyID, AIG_CourseNode node, bool enabled)
    {
        int nodeDist = Math.Min(node.m_playerCoverage.GetNodeDistanceToClosestPlayer(), 2);
        s_spawnQueues[nodeDist].Enqueue(new(enemyID, node, enabled));
        s_updateRoutine ??= CoroutineManager.StartCoroutine(SpawnUpdate().WrapToIl2Cpp());
    }

    static IEnumerator SpawnUpdate()
    {
        bool didSpawn;
        do
        {
            didSpawn = false;
            for (int i = 0; i < s_spawnQueues.Length; i++)
            {
                if (!s_spawnQueues[i].TryDequeue(out var spawn)) continue;

                var pos = spawn.Position;
                if (spawn.Random && !TryGetValidPosition(spawn.Node, spawn.Enabled, out pos))
                    continue;

                EnemyAllocator.Current.SpawnEnemy(spawn.EnemyID, spawn.Node, AgentMode.Hibernate, pos, spawn.Rotation);
                yield return s_spawnInterval[i];
                didSpawn = true;
                // Start from 0 again to check for closer spawns first
                i = -1;
            }
        } while (didSpawn);

        s_updateRoutine = null;
    }

    private static bool TryGetValidPosition(AIG_CourseNode node, bool enabled, out Vector3 pos)
    {
        bool isValidPos;
        var holder = s_placementHolders[node.NodeID];
        int attempts = 0;
        do
        {
            isValidPos = true;
            pos = holder.GetNextPosition();

            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                if (!player.Owner.IsBot && player.Position.IsWithinSqrDistance(pos, 12.25f)) // 3.5^2
                {
                    isValidPos = false;
                    Logger.Verbose(LogLevel.Debug, "A spawn position rerolled due to nearby player");
                    break;
                }
            }
        } while (!isValidPos && ++attempts < 10);

        if (!isValidPos && !enabled)
        {
            Logger.Verbose(LogLevel.Warning, "An enemy failed to spawn after maximum reroll attempts reached");
            return false;
        }

        return true;
    }

    private static void CleanupSpawnUpdate()
    {
        if (s_updateRoutine != null)
        {
            foreach (var queue in s_spawnQueues)
                queue.Clear();
            CoroutineManager.StopCoroutine(s_updateRoutine);
            s_updateRoutine = null;
        }
    }

    private static void HandlePlacementBatches(LG_Factory.BatchName batchName)
    {
        switch (batchName)
        {
            case LG_Factory.BatchName.AIGraph_CreateNodeClusters:
                break;
            case LG_Factory.BatchName.AIGraph_ScoreNodeClusters:
                foreach (var data in s_placementHolders)
                    data.SortNodes();
                return;
            case LG_Factory.BatchName.EnemiesPlacement_Scoring:
                AIG_CourseNode.s_allNodes[0].m_area.VoxelCoverage *= RundownManager.ActiveExpeditionBalanceData.VoxelCoverageAreaMultiplier;
                return;
            default:
                return;
        }

        s_currentScoredAreas = null!;
        s_placementHolders = new PlacementHolder[AIG_CourseNode.s_allNodes.Count];
        foreach (var node in AIG_CourseNode.s_allNodes)
        {
            var nodeID = node.NodeID;
            var cluster = node.m_nodeCluster;
            if (cluster == null)
            {
                s_placementHolders[nodeID] = null!;
                continue;
            }

            // NodeCluster.CreatePlacementList but trimmed to MaxPlacementNodes instead of 50.
            // Does not make the same order (nodes are given some random weight).
            var scoredPlacements = LG_Scoring.CreateScores(cluster.m_nodes, 0f);
            scoredPlacements = LG_Scoring.ScoreItems(scoredPlacements, (Func<AIG_INode, float>)Scorer_ValidNode, -1f);
            while (s_nodesToSpreadFrom.Count > 0)
            {
                AIG_INode aIG_INode = s_nodesToSpreadFrom.Dequeue();
                for (int i = 0; i < aIG_INode.Links.Count; i++)
                {
                    aIG_INode.Links[i].PlacementHeat += 1f;
                }
            }
            scoredPlacements = LG_Scoring.ScoreSort(scoredPlacements);
            scoredPlacements = LG_Scoring.SliceFromLowestScore(scoredPlacements, MaxPlacementNodes);
            var nodes = new AIG_INode[scoredPlacements.Count];
            for (int i = 0; i < nodes.Length; i++)
                nodes[i] = scoredPlacements[i].item;
            s_placementHolders[nodeID] = new(nodes);
        }
    }

    private static float Scorer_ValidNode(AIG_INode node)
    {
        if (node.HasSpecialTraversal || node.BelongsToGate)
        {
            s_nodesToSpreadFrom.Enqueue(node);
            return -10f;
        }
        if (!node.IsVoxelNode)
        {
            return -10f;
        }
        float num = 1f - Math.Clamp(node.Links.Count / 3f, 0f, 1f);
        float value = MasterRand.NextSingle();
        node.PlacementHeat = num + value;
        return node.PlacementHeat;
    }

    private static void ResetPlacements(LG_Zone zone)
    {
        foreach (var area in zone.m_areas)
        {
            var node = area.m_courseNode;
            var holder = s_placementHolders[node.NodeID];
            holder?.Reset();
            area.PlacedPopScore = 0;
        }
    }

    private static AIG_CourseNode GetRandomSpawnNode(WEE_SpawnHibernateData sh)
    {
        LG_Area best = LG_Scoring.GetHighestScored(LG_Scoring.ScoreSort(LG_Scoring.ScoreItems(s_currentScoredAreas, (Func<LG_Area, float>)Scorer_CoverageToPopulation, float.MinValue)));
        best.PlacedPopScore += sh.RandomPlacementScore;
        return best.m_courseNode;
    }

    private static float Scorer_CoverageToPopulation(LG_Area area)
    {
        return Mathf.Lerp(area.VoxelCoverage - area.PlacedPopScore, MasterRand.NextSingle(), s_currentRandomWeight);
    }
}
