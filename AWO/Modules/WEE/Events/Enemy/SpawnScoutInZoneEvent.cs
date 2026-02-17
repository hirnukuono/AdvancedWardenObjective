using AIGraph;
using Enemies;
using GameData;
using LevelGeneration;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal class SpawnScoutInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SpawnScoutInZone;
    public override bool AllowArrayableGlobalIndex => true;

    private const float TimeToCompleteSpawn = 2f;

    protected override void TriggerMaster(WEE_EventData e) 
    {
        if (!TryGetZone(e, out var zone)) 
            return;

        foreach (var ss in e.SpawnScouts.Values)
        {
            if (ss.AreaIndex == -1 || IsValidAreaIndex(ss.AreaIndex, zone))
            {
                Vector3 pos = GetPositionFallback(e.Position, e.SpecialText, false);
                int count = ResolveFieldsFallback(e.Count, ss.Count);
                CoroutineManager.StartCoroutine(DoSpawn(ss, zone, pos, count).WrapToIl2Cpp());
            }
        }
    }

    static IEnumerator DoSpawn(WEE_SpawnScoutData ss, LG_Zone zone, Vector3 pos, int count)
    {
        float interval = Math.Min(0.25f, TimeToCompleteSpawn / count); // max 4 enemies per second
        WaitForSeconds spawnInterval = new(interval);
        var areas = zone.m_areas;
        var validAreas = Enumerable.Range(0, areas.Count).Except(ss.AreaBlacklist).ToList();
        
        if (validAreas.Count == 0)
        {
            Logger.Error("SpawnScoutInZoneEvent", $"No valid areas to spawn scout! Area count: {areas.Count}, Blacklist: [{string.Join(", ", ss.AreaBlacklist)}]");
            yield break;
        }

        for (int spawnCount = 0; spawnCount < count; spawnCount++)
        {
            if (!EnemySpawnManager.TryCreateEnemyGroupRandomizer(ss.GroupType, ss.Difficulty, out EnemyGroupRandomizer? r))
            {
                Logger.Error("SpawnScoutInZoneEvent", $"Invalid EnemyGroup: (GroupType: {ss.GroupType}, Difficulty: {ss.Difficulty})");
                yield break;
            }

            EnemyGroupDataBlock randomGroup = r.GetRandomGroup(MasterRand.NextFloat());
            float popPoints = randomGroup.MaxScore * MasterRand.NextRange(1f, 1.2f);

            AIG_CourseNode spawnNode;
            if (ss.AreaIndex != -1)
            {
                spawnNode = areas[ss.AreaIndex].m_courseNode;
            }
            else
            {                
                int randArea = validAreas[MasterRand.Next(validAreas.Count)];
                spawnNode = areas[randArea].m_courseNode;
            }

            var scoutSpawnData = EnemyGroup.GetSpawnData
            (
                pos == Vector3.zero ? spawnNode.GetRandomPositionInside() : pos, 
                spawnNode, 
                EnemyGroupType.Hibernating, 
                eEnemyGroupSpawnType.RandomInArea, 
                randomGroup.persistentID, 
                popPoints
            ) with { respawn = false };

            EnemyGroup.Spawn(scoutSpawnData);
            yield return spawnInterval;
        }
    }
}
