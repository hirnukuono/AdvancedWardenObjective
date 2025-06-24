using Agents;
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
            yield break;
        }

        WOManager.DisplayWardenIntel(e.Layer, e.WardenIntel);

        if (e.DialogueID > 0u)
        {
            PlayerDialogManager.WantToStartDialog(e.DialogueID, -1, false, false);
        }

        switch (type)
        {
            case VEO_Type.PlaySound:
                PlaySound(e);
                break;
            case VEO_Type.SpawnEnemyOnPoint:
                CoroutineManager.StartCoroutine(SpawnEnemyOnPoint(e).WrapToIl2Cpp());
                break;
        }
    }

    private static void PlaySound(WardenObjectiveEventData e)
    {
        if (e.SoundID == 0u) return;
        
        CellSoundPlayer soundEvent = new();
        soundEvent.Post(e.SoundID, e.Position, 1u, (AkEventCallback)SoundDoneCallback, soundEvent);

        var line = e.SoundSubtitle.ToString();
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

    static IEnumerator SpawnEnemyOnPoint(WardenObjectiveEventData e)
    {
        if (e.SoundID > 0u)
        {
            WOManager.Current.m_sound.Post(e.SoundID, true);
            var line = e.SoundSubtitle.ToString();
            if (!string.IsNullOrWhiteSpace(line))
            {
                GuiManager.PlayerLayer.ShowMultiLineSubtitle(line);
            }
        }

        if (!SNet.IsMaster) yield break;

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
