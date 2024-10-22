using Agents;
using AIGraph;
using AWO.WEE.Events;
using Enemies;
using LevelGeneration;
using Player;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AWO.Modules.WEE.Events.Enemy;
internal class SpawnHibernateInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SpawnHibernateInZone;

    private const float TimeToCompleteSpawn = 2.0f;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone) || zone == null) 
            return;

        var sh = e.SpawnHibernates;
        if (sh.AreaIndex == -1 || IsValidAreaIndex(sh.AreaIndex, zone))
        {
            if (sh.Count == 1 && sh.Position != Vector3.zero) // spawn 1 enemy at a specific position
            {
                EnemyAllocator.Current.SpawnEnemy(sh.EnemyID, zone.m_areas[sh.AreaIndex].m_courseNode, AgentMode.Hibernate, sh.Position, Quaternion.Euler(sh.Rotation));
            }
            else
            {
                CoroutineManager.StartCoroutine(DoSpawn(sh, zone, e.Enabled).WrapToIl2Cpp());
            }
        }
    }

    static IEnumerator DoSpawn(WEE_SpawnHibernateData sh, LG_Zone zone, bool enabled)
    {
        AgentMode mode = AgentMode.Hibernate;
        WaitForSeconds spawnInterval = new(TimeToCompleteSpawn / sh.Count);

        var rand = new System.Random();
        var areas = zone.m_areas;

        for (int spawnCount = 0; spawnCount < sh.Count; spawnCount++)
        {
            AIG_CourseNode spawnNode = sh.AreaIndex != -1 ? areas[sh.AreaIndex].m_courseNode : areas[rand.Next(0, areas.Count)].m_courseNode;
            Vector3 pos;
            int attempts = 0;
            bool isValidPos;

            do
            {
                isValidPos = true;
                pos = spawnNode.GetRandomPositionInside();

                foreach (var player in PlayerManager.PlayerAgentsInLevel)
                {
                    if (Vector3.Distance(player.Position, pos) < 3.5f)
                    {
                        isValidPos = false;
                        //Logger.Debug("SpawnHibernates - spawn pos rerolling due to pos conflict");
                        break;
                    }
                }

                if (isValidPos)
                {
                    foreach (var enemy in spawnNode.m_enemiesInNode)
                    {
                        if (Vector3.Distance(enemy.Position, pos) < 2.3f)
                        {
                            isValidPos = false;
                            //Logger.Debug("SpawnHibernates - spawn pos rerolling due to pos conflict");
                            break;
                        }
                    }
                }
            } while (!isValidPos && attempts++ < 5);

            if (!isValidPos && !enabled)
                continue;

            Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            EnemyAllocator.Current.SpawnEnemy(sh.EnemyID, spawnNode, mode, pos, rotation);

            yield return spawnInterval;
        }
    }

    private static bool IsValidAreaIndex(int areaIndex, LG_Zone zone)
    {
        var areas = zone.m_areas;
        if (areaIndex < 0 || areaIndex >= areas.Count)
        {
            Logger.Error($"[SpawnHibernateInZoneEvent] Invalid area index {areaIndex}");
            return false;
        }

        return true;
    }
}
