using Agents;
using AIGraph;
using Enemies;
using LevelGeneration;
using Player;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal class SpawnHibernateInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SpawnHibernateInZone;

    private const float TimeToCompleteSpawn = 2.0f;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) return;

        var sh = e.SpawnHibernates;
        if (sh.AreaIndex == -1 || IsValidAreaIndex(sh.AreaIndex, zone))
        {
            Vector3 pos = GetPositionFallback(ResolveFieldsFallback(e.Position, sh.Position, false), e.SpecialText, false);
            int count = ResolveFieldsFallback(e.Count, sh.Count);

            if (count == 1 && pos != Vector3.zero) // spawn 1 enemy at a specific position
            {
                EnemyAllocator.Current.SpawnEnemy(sh.EnemyID, zone.m_areas[sh.AreaIndex].m_courseNode, AgentMode.Hibernate, pos, Quaternion.Euler(sh.Rotation));
            }
            else
            {
                CoroutineManager.StartCoroutine(DoSpawn(sh, zone, count, e.Enabled).WrapToIl2Cpp());
            }
        }
    }

    static IEnumerator DoSpawn(WEE_SpawnHibernateData sh, LG_Zone zone, int count, bool enabled)
    {
        AgentMode mode = AgentMode.Hibernate;
        WaitForSeconds spawnInterval = new(TimeToCompleteSpawn / count);

        var areas = zone.m_areas;

        for (int spawnCount = 0; spawnCount < count; spawnCount++)
        {
            AIG_CourseNode spawnNode = sh.AreaIndex != -1 ? areas[sh.AreaIndex].m_courseNode : areas[MasterRand.Next(areas.Count)].m_courseNode;
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

            if (!isValidPos && !enabled) continue;

            Quaternion rotation = Quaternion.Euler(0, MasterRand.NextRange(0, 360), 0);
            EnemyAllocator.Current.SpawnEnemy(sh.EnemyID, spawnNode, mode, pos, rotation);

            yield return spawnInterval;
        }
    }
}
