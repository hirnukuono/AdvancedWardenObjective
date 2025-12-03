using Agents;
using AmorLib.Utils;
using AmorLib.Utils.Extensions;
using BepInEx.Logging;
using Enemies;
using GameData;
using LevelGeneration;
using Player;
using SNetwork;
using System.Collections;
using UnityEngine;
using VEO_Type = GameData.eWardenObjectiveEventType;

namespace AWO.Modules.WEE;

internal static class VanillaEventOvr
{
    internal static bool HasOverride(VEO_Type type, WardenObjectiveEventData e)
    {
        bool overridePos = e.Position != Vector3.zero;
        return type switch
        {
            VEO_Type.AllLightsOn or VEO_Type.AllLightsOff => e.DimensionIndex != eDimensionIndex.Reality,
            VEO_Type.PlaySound => overridePos,
            VEO_Type.SpawnEnemyOnPoint => overridePos || e.Count > 0,
            _ => false,
        };
    }

    internal static void HandleEvent(VEO_Type type, WardenObjectiveEventData e, float currentDuration) => CoroutineManager.StartCoroutine(Handle(type, e, currentDuration).WrapToIl2Cpp());

    private static IEnumerator Handle(VEO_Type type, WardenObjectiveEventData e, float currentDuration)
    {
        float delay = Mathf.Max(e.Delay - currentDuration, 0f);
        if (delay > 0f)
        {
            int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
            yield return new WaitForSeconds(delay);
            if (reloadCount < CheckpointManager.Current.m_stateReplicator.State.reloadCount)
            {
                Logger.Warn($"Delayed event {type} aborted due to checkpoint reload");
                yield break;
            }
        }

        if (WorldEventManager.GetCondition(e.Condition.ConditionIndex) != e.Condition.IsTrue)
        {
            Logger.Verbose(LogLevel.Debug, $"Condition {e.Condition.ConditionIndex} is not met");
            yield break;
        }

        WOManager.DisplayWardenIntel(e.Layer, e.WardenIntel);

        if (e.DialogueID > 0u)
        {
            PlayerDialogManager.WantToStartDialog(e.DialogueID, -1, false, false);
        }

        if (e.SoundID > 0u)
        {
            PlaySound(e);
        }

        if (SNet.IsMaster) // all these events are host-only
        {
            switch (type)
            {
                case VEO_Type.AllLightsOn:
                    ToggleDimensionLights(true, e.DimensionIndex);
                    break;

                case VEO_Type.AllLightsOff:
                    ToggleDimensionLights(false, e.DimensionIndex);
                    break;

                case VEO_Type.PlaySound:
                    break;

                case VEO_Type.SpawnEnemyOnPoint:
                    CoroutineManager.StartCoroutine(SpawnEnemyOnPoint(e).WrapToIl2Cpp());
                    break;
            }
        }
    }

    private static void PlaySound(WardenObjectiveEventData e)
    {        
        if (e.Type != VEO_Type.PlaySound)
        {
            WOManager.Current.m_sound.Post(e.SoundID, true);
        }
        else
        {
            CellSoundPlayer soundEvent = new();
            soundEvent.PostWithCleanup(e.SoundID, e.Position);
        }

        string line = e.SoundSubtitle.ToString();
        if (!string.IsNullOrWhiteSpace(line))
        {
            GuiManager.PlayerLayer.ShowMultiLineSubtitle(line);
        }
    }
    
    private static void ToggleDimensionLights(bool mode, eDimensionIndex dimension)
    {
        pEnvironmentInteraction state = default;
        state.EnvironmentStateChangeType = EnvironmentStateChangeType.LightModeAll;
        state.LightsEnabled = mode;
        state.DimensionIndex = dimension;
        pEnvironmentInteraction interaction = state;
        EnvironmentStateManager.LogEnvironmentState("VEO SetLightMode Attempt");
        EnvironmentStateManager.Current.AttemptInteract(interaction);
    }

    static IEnumerator SpawnEnemyOnPoint(WardenObjectiveEventData e)
    {
        int count = e.Count < 2 ? 1 : e.Count;

        Vector3 pos = WorldEventUtils.TryGetRandomWorldEventObjectFromFilter(e.WorldEventObjectFilter, (uint)Builder.SessionSeedRandom.Seed, out var weObject)
            ? weObject.gameObject.transform.position
            : e.Position;
        var courseNode = CourseNodeUtil.GetCourseNode(pos, pos.GetDimension().DimensionIndex);
        if (courseNode == null)
        {
            Logger.Error("SpawnEnemyOnPoint", "Failed to find valid CourseNode from Position!");
            yield break;
        }

        AgentMode mode = e.Enabled switch 
        {
            true => AgentMode.Agressive,
            false when (e.EnemyID == 20) => AgentMode.Scout, // mimicks r7 vanilla behavior?
            _ => AgentMode.Hibernate
        };

        float interval = Math.Min(0.25f, 2.0f / count); // max 4 enemies per second
        WaitForSeconds spawnInterval = new(interval); 
        for (int i = 0; i < count; i++)
        {
            EnemyAgent.SpawnEnemy(e.EnemyID, pos, courseNode, mode);
            yield return spawnInterval;
        }
    }
}
