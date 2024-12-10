using Agents;
using AIGraph;
using LevelGeneration;
using Player;

namespace AWO.Modules.WEE.Events;

internal sealed class AlertEnemiesInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.AlertEnemiesInZone;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) return;

        if (e.SpecialNumber == -1)
        {
            foreach (var node in zone.m_courseNodes)
            {
                DoAlert(node, e.Enabled);
            }
        }
        else if (IsValidAreaIndex(e.SpecialNumber, zone))
        {
            DoAlert(zone.m_areas[e.SpecialNumber].m_courseNode, e.Enabled);
        }
    }

    private static void DoAlert(AIG_CourseNode node, bool enabled)
    {
        if (node.m_enemiesInNode == null) return;

        if (enabled)
        {
            foreach (var door in node.gameObject.GetComponentsInChildren<LG_WeakDoor>())
            {
                door.TriggerOperate(true);
                door.m_sync.AttemptDoorInteraction(eDoorInteractionType.Open, 0, 0);
            }
        }

        foreach (var enemy in node.m_enemiesInNode)
        {
            PlayerManager.TryGetClosestAlivePlayerAgent(enemy.CourseNode, out PlayerAgent minae);
            AgentMode mode = AgentMode.Agressive;
            enemy.AI.SetStartMode(mode);
            enemy.AI.ModeChange();
            enemy.AI.m_mode = mode;
            enemy.AI.SetDetectedAgent(minae, AgentTargetDetectionType.DamageDetection);
        }
    }
}
