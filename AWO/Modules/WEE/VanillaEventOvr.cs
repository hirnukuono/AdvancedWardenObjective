using Agents;
using BepInEx.Logging;
using Enemies;
using GameData;
using LevelGeneration;
using Player;
using SNetwork;
using System.Collections;
using UnityEngine;
using AkEventCallback = AkCallbackManager.EventCallback;
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
            yield return new WaitForSeconds(delay);
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
        if (e.Type != VEO_Type.PlaySound || e.Position == Vector3.zero)
        {
            WOManager.Current.m_sound.Post(e.SoundID, true);
        }
        else
        {
            CellSoundPlayer soundEvent = new();
            soundEvent.Post(e.SoundID, e.Position, 1u, (AkEventCallback)SoundDoneCallback, soundEvent);
        }        

        string line = e.SoundSubtitle.ToString();
        if (!string.IsNullOrWhiteSpace(line))
        {
            GuiManager.PlayerLayer.ShowMultiLineSubtitle(line);
        }
    }

    private static void SoundDoneCallback(Il2CppSystem.Object in_cookie, AkCallbackType in_type, AkCallbackInfo callbackInfo)
    {
        var callbackPlayer = in_cookie.Cast<CellSoundPlayer>();
        callbackPlayer?.Recycle();
    }
    
    private static void ToggleDimensionLights(bool mode, eDimensionIndex dimension)
    {
        pEnvironmentInteraction state = default;
        state.EnvironmentStateChangeType = EnvironmentStateChangeType.LightModeAll;
        state.LightsEnabled = mode;
        state.DimensionIndex = dimension;
        pEnvironmentInteraction interaction = state;
        EnvironmentStateManager.LogEnvironmentState("SetLightMode Attempt");
        EnvironmentStateManager.Current.AttemptInteract(interaction);
    }

    static IEnumerator SpawnEnemyOnPoint(WardenObjectiveEventData e)
    {
        int count = e.Count < 2 ? 1 : e.Count;

        Vector3 pos = WorldEventUtils.TryGetRandomWorldEventObjectFromFilter(e.WorldEventObjectFilter, (uint)Builder.SessionSeedRandom.Seed, out var weObject)
            ? weObject.gameObject.transform.position
            : e.Position;
        if (!Dimension.TryGetCourseNodeFromPos(pos, out var courseNode))
        {
            Logger.Error("SpawnEnemyOnPoint", "Failed to find valid CourseNode from Position!");
            yield break;
        }

        AgentMode mode = e.Enabled switch 
        {
            true => AgentMode.Agressive,
            false when (e.EnemyID == 20) => AgentMode.Scout, // mimicks vanilla behavior
            _ => AgentMode.Hibernate
        };

        WaitForSeconds spawnInterval = new(2.0f / count);
        for (int i = 0; i < count; i++)
        {
            EnemyAgent.SpawnEnemy(e.EnemyID, pos, courseNode, mode);
            yield return spawnInterval;
        }
    }
}
