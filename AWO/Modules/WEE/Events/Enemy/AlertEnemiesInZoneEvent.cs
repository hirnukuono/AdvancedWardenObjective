using Agents;
using AWO.Modules.WEE;
using LevelGeneration;
using Player;

namespace AWO.WEE.Events.Enemy;

internal sealed class AlertEnemiesInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.AlertEnemiesInZone;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone))
        {
            LogError("Zone is Missing?");
            return;
        }

        foreach (var node in zone.m_courseNodes)
        {
            if (node.m_enemiesInNode == null)
                continue;

            if (e.Enabled)
            {
                foreach (var door in node.gameObject.GetComponentsInChildren<LG_WeakDoor>())
                {
                    door.TriggerOperate(true);
                    door.m_sync.AttemptDoorInteraction(eDoorInteractionType.Open, 0, 0);
                }
            }

            foreach (var enemy in node.m_enemiesInNode)
            {
                
                PlayerAgent minae;
                PlayerManager.TryGetLocalPlayerAgent(out minae);
                AgentMode mode = Agents.AgentMode.Agressive;
                enemy.AI.SetStartMode(mode);
                enemy.AI.ModeChange();
                enemy.AI.m_mode = mode;
                enemy.AI.SetDetectedAgent(minae, AgentTargetDetectionType.DamageDetection);
                
                /*
                var mode2 = enemy.AI.Mode;
                if (mode2 == AgentMode.Hibernate)
                {
                    if (enemy.CourseNode.m_playerCoverage.GetNodeDistanceToClosestPlayer_Unblocked() > 2)
                    {
                        enemy.AI.m_behaviour.ChangeState(Enemies.EB_States.InCombat);
                    }
                    else
                    {
                        var delta = (LocalPlayer.Position - enemy.Position);
                        enemy.Locomotion.HibernateWakeup.ActivateState(delta.normalized, delta.magnitude, 0.0f, false);
                    }
                }
                else if (mode == AgentMode.Scout)
                {
                    enemy.Locomotion.ScoutScream.ActivateState(enemy.AI.m_behaviourData.GetTarget(LocalPlayer));
                }*/
            }
        }
    }
}
