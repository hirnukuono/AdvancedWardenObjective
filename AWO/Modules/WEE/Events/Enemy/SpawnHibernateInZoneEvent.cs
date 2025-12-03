using Agents;
using AIGraph;
using AmorLib.Utils.Extensions;
using BepInEx.Logging;
using Enemies;
using LevelGeneration;
using Player;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal class SpawnHibernateInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SpawnHibernateInZone;
    public override bool WhitelistArrayableGlobalIndex => true;
    private const float TimeToCompleteSpawn = 2.0f;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) return;

        foreach (var sh in e.SpawnHibernates.Values)
        { 
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
    }

    static IEnumerator DoSpawn(WEE_SpawnHibernateData sh, LG_Zone zone, int count, bool enabled)
    {
        float interval = Math.Min(0.25f, TimeToCompleteSpawn / count); // max 4 enemies per second
        WaitForSeconds spawnInterval = new(interval);
        var areas = zone.m_areas;

        for (int spawnCount = 0; spawnCount < count; spawnCount++)
        {
            AIG_CourseNode spawnNode;
            Vector3 pos;
            int attempts = 0;
            bool isValidPos;
            
            if (sh.AreaIndex != -1)
            {
                spawnNode = areas[sh.AreaIndex].m_courseNode;
            }
            else
            {
                var validAreas = Enumerable.Range(0, areas.Count).Except(sh.AreaBlacklist).ToList();
                if (validAreas.Count == 0)
                {
                    Logger.Error("SpawnHibernateInZoneEvent", $"No valid areas to spawn hibernate! Area count: {areas.Count}, Blacklist: [{string.Join(", ", sh.AreaBlacklist)}]");
                    yield break;
                }
                int randArea = validAreas[MasterRand.Next(validAreas.Count)];
                spawnNode = areas[randArea].m_courseNode;
            }

            do
            {
                isValidPos = true;
                pos = spawnNode.GetRandomPositionInside();

                foreach (var player in PlayerManager.PlayerAgentsInLevel)
                {
                    if (!player.Owner.IsBot && player.Position.IsWithinSqrDistance(pos, 12.25f)) // 3.5^2
                    {
                        isValidPos = false;
                        Logger.Verbose(LogLevel.Debug, "A spawn position rerolled due to nearby player");
                        break;
                    }
                }

                if (isValidPos)
                {
                    foreach (var enemy in spawnNode.m_enemiesInNode)
                    {
                        if (enemy.Position.IsWithinSqrDistance(pos, 5.29f)) // 2.3^2
                        {
                            isValidPos = false;
                            Logger.Verbose(LogLevel.Debug, "A spawn position rerolled due to nearby enemy");
                            break;
                        }
                    }
                }
            } while (!isValidPos && ++attempts < 5);

            if (!isValidPos && !enabled)
            {
                Logger.Verbose(LogLevel.Warning, "An enemy failed to spawn after maximum reroll attempts reached");
                continue;
            }

            Quaternion rotation = Quaternion.Euler(0, MasterRand.NextRange(0, 360), 0);
            EnemyAllocator.Current.SpawnEnemy(sh.EnemyID, spawnNode, AgentMode.Hibernate, pos, rotation);

            yield return spawnInterval;
        }
    }
}
