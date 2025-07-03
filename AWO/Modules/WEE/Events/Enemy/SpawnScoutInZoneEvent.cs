using Enemies;
using GameData;
using LevelGeneration;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events;
internal class SpawnScoutInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SpawnScoutInZone;

    private const float TimeToCompleteSpawn = 2.0f;

    protected override void TriggerMaster(WEE_EventData e) 
    {
        if (!TryGetZone(e, out var zone)) return;

        var ss = e.SpawnScouts;
        if (ss.AreaIndex == -1 || IsValidAreaIndex(ss.AreaIndex, zone))
        {
            Vector3 pos = GetPositionFallback(e.Position, e.SpecialText, false);
            int count = ResolveFieldsFallback(e.Count, ss.Count);

            CoroutineManager.StartCoroutine(DoSpawn(ss, zone, pos, count).WrapToIl2Cpp());
        }
    }

    static IEnumerator DoSpawn(WEE_SpawnScoutData ss, LG_Zone zone, Vector3 pos, int count)
    {
        WaitForSeconds spawnInterval = new(TimeToCompleteSpawn / count);

        for (int SpawnCount = 0; SpawnCount < count; SpawnCount++)
        {
            if (!EnemySpawnManager.TryCreateEnemyGroupRandomizer(ss.GroupType, ss.Difficulty, out EnemyGroupRandomizer? r))
            {
                Logger.Error("SpawnScoutInZoneEvent", $"Invalid EnemyGroup: (GroupType: {ss.GroupType}, Difficulty: {ss.Difficulty})");
                yield break;
            }

            EnemyGroupDataBlock randomGroup = r.GetRandomGroup(MasterRand.NextFloat());
            float popPoints = randomGroup.MaxScore * MasterRand.NextRange(1.0f, 1.2f);
            var node = ss.AreaIndex != -1 ? zone.m_areas[ss.AreaIndex].m_courseNode : zone.m_areas[MasterRand.Next(zone.m_areas.Count)].m_courseNode;

            var scoutSpawnData = EnemyGroup.GetSpawnData
            (
                pos == Vector3.zero ? node.GetRandomPositionInside() : pos, 
                node, 
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
