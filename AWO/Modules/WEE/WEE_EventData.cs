using Agents;
using AIGraph;
using AWO.Jsons;
using Enemies;
using GameData;
using LevelGeneration;
using SNetwork;
using System;
using System.Linq;
using UnityEngine;

namespace AWO.Modules.WEE;

public sealed class WEE_EventData
{
    public WEE_Type Type { get; set; }

    //Vanilla Fields for Serialization
    public WorldEventConditionPair Condition { get; set; } = new()
    {
        ConditionIndex = -1,
        IsTrue = false
    };
    public uint ChainPuzzle { get; set; } = 0u;
    public bool UseStaticBioscanPoints { get; set; } = false;
    public eWardenObjectiveEventTrigger Trigger { get; set; } = eWardenObjectiveEventTrigger.None;

    //Common Fields
    public eDimensionIndex DimensionIndex { get; set; } = eDimensionIndex.Reality;
    public LG_LayerType Layer { get; set; } = LG_LayerType.MainLayer;
    public eLocalZoneIndex LocalIndex { get; set; } = eLocalZoneIndex.Zone_0;
    public Vector3 Position { get; set; } = Vector3.zero;
    public float Delay { get; set; } = 0.0f;
    public float Duration { get; set; } = 0.0f;
    public bool ClearDimension { get; set; } = false;
    public LocaleText WardenIntel { get; set; } = LocaleText.Empty;
    public uint SoundID { get; set; } = 0u;
    public LocaleText SoundSubtitle { get; set; } = LocaleText.Empty;
    public uint DialogueID { get; set; } = 0u;
    public int Count { get; set; } = 0;
    public bool Enabled { get; set; } = true;


    //Common Updater
    public WEE_SubObjectiveData SubObjective { get; set; } = new();
    public WEE_UpdateFogData Fog { get; set; } = new();

    //Command Specific
    public WEE_ReactorEventData Reactor { get; set; } = new();
    public WEE_CountdownData Countdown { get; set; } = new();
    public WEE_CleanupEnemiesData CleanupEnemies { get; set; } = new();
    public WEE_ZoneLightData SetZoneLight { get; set; } = new();
    public bool CleanUpEnemiesBehind { get; set; } = true;
    public WEE_SpawnHibernateData SpawnHibernates { get; set; } = new();
    public WEE_SpawnScoutData SpawnScouts { get; set; } = new();

    // hirnu
    public WEE_AddTerminalCommand AddTerminalCommand { get; set; } = new();
    public WEE_HideTerminalCommand HideTerminalCommand { get; set; } = new();
    public WEE_UnhideTerminalCommand UnhideTerminalCommand { get; set; } = new();

    // amor
    public WEE_NestedEvent NestedEvent { get; set; } = new();
    public WEE_StartEventLoop StartEventLoop { get; set; } = new();
    public WEE_StartEventLoop StopEventLoop { get; set; } = new();
    public WEE_TeleportPlayer TeleportPlayer { get; set; } = new();
    public WEE_InfectPlayer InfectPlayer { get; set; } = new();
    public WEE_DamagePlayer DamagePlayer { get; set; } = new();
    public WEE_RevivePlayer RevivePlayer { get; set; } = new();
    public WEE_Countup Countup { get; set; } = new();

    public WardenObjectiveEventData CreateDummyEventData()
    {
        return new()
        {
            Type = (eWardenObjectiveEventType)(int)Type,
            ChainPuzzle = ChainPuzzle,
            UseStaticBioscanPoints = UseStaticBioscanPoints,
            Trigger = Trigger,
            Condition = Condition
        };
    }
}

public sealed class WEE_UpdateFogData
{
    public bool DoUpdate { get; set; } = false;
    public uint FogSetting { get; set; }
    public float FogTransitionDuration { get; set; }
}

public sealed class WEE_SubObjectiveData
{
    public bool DoUpdate { get; set; } = false;
    public LocaleText CustomSubObjectiveHeader { get; set; } = LocaleText.Empty;
    public LocaleText CustomSubObjective { get; set; } = LocaleText.Empty;
}

public sealed class WEE_ReactorEventData
{
    public WaveState State { get; set; } = WaveState.Intro;
    public int Wave { get; set; } = 1;
    public float Progress { get; set; } = 0.0f;

    public enum WaveState
    {
        Intro,
        Wave,
        Verify
    }
}

public sealed class WEE_DoorInteractionData
{
    public bool LockdownState { get; set; }
    public string LockdownMessage { get; set; }
}

public sealed class WEE_CountdownData
{
    public float Duration { get; set; }
    public LocaleText TimerText { get; set; } = LocaleText.Empty;
    public Color TimerColor { get; set; } = Color.red;
    public WardenObjectiveEventData[] EventsOnDone { get; set; } = Array.Empty<WardenObjectiveEventData>();
}

public sealed class WEE_CleanupEnemiesData
{
    public CleanUpType Type { get; set; } = CleanUpType.Despawn;
    public bool IncludeHibernate { get; set; } = true;
    public bool IncludeAggressive { get; set; } = true;
    public bool IncludeScout { get; set; } = true;
    public uint[] ExcludeEnemyID { get; set; } = Array.Empty<uint>();

    public uint[] IncludeOnlyID { get; set; } = Array.Empty<uint>();

    public void DoClear(AIG_CourseNode node)
    {
        if (!SNet.IsMaster)
            return;

        if (node == null)
            return;

        if (node.m_enemiesInNode == null)
            return;

        List<EnemyAgent> enemylist = new();
        enemylist.Clear();

        foreach (EnemyAgent enemy in node.m_enemiesInNode) enemylist.Add(enemy);

        foreach (EnemyAgent enemy in enemylist)
        {
            bool clear = enemy.AI.Mode switch
            {
                AgentMode.Agressive => IncludeAggressive,
                AgentMode.Scout => IncludeScout,
                AgentMode.Hibernate => IncludeHibernate,
                _ => true
            };

            if (clear)
            {
                if (ExcludeEnemyID.Contains(enemy.EnemyDataID))
                {
                    continue;
                }

                if (IncludeOnlyID.Length > 0 && !IncludeOnlyID.Contains(enemy.EnemyDataID)) continue;

                switch (Type)
                {
                    case CleanUpType.Despawn:
                        enemy.m_replicator.Despawn();
                        break;

                    case CleanUpType.Kill:
                        enemy.Damage.IsImortal = false;
                        enemy.Damage.BulletDamage(enemy.Damage.DamageMax, null, default, default, default);
                        break;
                }
            }
        }
    }

    public enum CleanUpType
    {
        Kill,
        Despawn
    }
}

public sealed class WEE_ZoneLightData
{
    public ModifierType Type { get; set; } = ModifierType.RevertToOriginal;
    public uint LightDataID { get; set; }
    public float TransitionDuration { get; set; } = 0.5f;
    public int Seed { get; set; } = 0; //Random on Zero

    public enum ModifierType : byte
    {
        RevertToOriginal,
        SetZoneLightData
    }
}

public sealed class WEE_SpawnHibernateData
{
    public int AreaIndex { get; set; } = -1; // -1 denotes 'random'
    public uint EnemyID { get; set; } = 0;
    public uint Count { get; set; } = 1;
    public Vector3 Position { get; set; } = Vector3.zero;
    public Vector3 Rotation { get; set; } = Vector3.zero;
}

public sealed class WEE_SpawnScoutData
{
    public int AreaIndex { get; set; } = -1; // -1 denotes 'random'
    public eEnemyGroupType GroupType { get; set; }
    public eEnemyRoleDifficulty Difficulty { get; set; }
    public uint Count { get; set; } = 1;
}

public enum FilterMode
{
    Exclude,
    Include,
}

public sealed class WEE_AddTerminalCommand
{
    public int TerminalIndex { get; set; } = 0;
    public int CommandNumber { get; set; } = 6;
    public string Command { get; set; } = "";
    public string CommandDesc { get; set; } = "";
    public TERM_CommandRule SpecialCommandRule { get; set; } = TERM_CommandRule.Normal;
    public WardenObjectiveEventData[] CommandEvents { get; set; } = Array.Empty<WardenObjectiveEventData>();
    public TerminalOutput[] PostCommandOutputs { get; set; } = Array.Empty<TerminalOutput>();
}
public sealed class WEE_HideTerminalCommand
{
    public int TerminalIndex { get; set; } = 0; 
    public TERM_Command CommandEnum { get; set; } = TERM_Command.None;
    public int CommandNumber { get; set; } = new();
    public bool DeleteCommand { get; set; } = false;
}
public sealed class WEE_UnhideTerminalCommand
{
    public int TerminalIndex { get; set; } = 0; 
    public TERM_Command CommandEnum { get; set; } = TERM_Command.None;

    public int CommandNumber { get; set; } = new();
}


public sealed class WEE_NestedEvent
{
    public WardenObjectiveEventData[] EventsToActivate { get; set; } = Array.Empty<WardenObjectiveEventData>();
}
public sealed class WEE_StartEventLoop
{
    public int LoopIndex { get; set; } = 0;
    public float LoopDelay { get; set; } = 1.0f;
    public int LoopCount { get; set; } = -1;
    public WardenObjectiveEventData[] EventsToActivate { get; set; } = Array.Empty<WardenObjectiveEventData>();
}
public enum SlotIndex : byte
{
    P0,
    P1,
    P2,
    P3
}
public sealed class WEE_TeleportPlayer
{
    public HashSet<SlotIndex> PlayerFilter { get; set; } = new();
    public bool PlayWarpAnimation { get; set; } = true;
    public bool FlashTeleport { get; set; } = false;
    public bool WarpSentries { get; set; } = true;
    public bool WarpBigPickups { get; set; } = true;
    public bool SendBPUsToHost {  get; set; } = false;
    public Vector3 Player0Position { get; set; } = Vector3.zero;
    public int P0LookDir { get; set; } = 0;
    public Vector3 Player1Position { get; set; } = Vector3.zero;
    public int P1LookDir { get; set; } = 0;
    public Vector3 Player2Position { get; set; } = Vector3.zero;
    public int P2LookDir { get; set; } = 0;
    public Vector3 Player3Position { get; set; } = Vector3.zero;
    public int P3LookDir { get; set; } = 0;
}
public sealed class WEE_InfectPlayer
{
    public HashSet<SlotIndex> PlayerFilter { get; set; } = new HashSet<SlotIndex> { SlotIndex.P0, SlotIndex.P1, SlotIndex.P2, SlotIndex.P3};
    public float InfectionAmount { get; set; } = 0.0f;
    public bool InfectOverTime { get; set; } = false;
    public bool UseZone { get; set; } = false;
}
public sealed class WEE_DamagePlayer
{
    public HashSet<SlotIndex> PlayerFilter { get; set; } = new HashSet<SlotIndex> { SlotIndex.P0, SlotIndex.P1, SlotIndex.P2, SlotIndex.P3 };
    public float DamageAmount { get; set; } = 0.0f;
    public bool DamageOverTime { get; set; } = false;
    public bool UseZone { get; set; } = false;
}
public sealed class WEE_RevivePlayer
{
    public HashSet<SlotIndex> PlayerFilter { get; set; } = new HashSet<SlotIndex> { SlotIndex.P0, SlotIndex.P1, SlotIndex.P2, SlotIndex.P3 };
}
public sealed class WEE_Countup
{
    public enum CountupMode : byte
    {
        Stopwatch,
        Counter
    }
    public LocaleText TimerText { get; set; } = LocaleText.Empty;
    public Color TimerColor { get; set; } = Color.red;
    public LocaleText CustomText { get; set; } = LocaleText.Empty;
    public float SpeedMultiplier { get; set; } = 0.1f;
    public WardenObjectiveEventData[] EventsOnDone { get; set; } = Array.Empty<WardenObjectiveEventData>();
}