using Agents;
using AIGraph;
using AWO.Jsons;
using Enemies;
using GameData;
using LevelGeneration;
using Localization;
using Player;
using SNetwork;
using System.Text.Json.Serialization;
using UnityEngine;

namespace AWO.Modules.WEE;

public sealed class WEE_EventData
{
    // Vanilla Fields for Serialization
    public WEE_Type Type { get; set; }    
    public WorldEventConditionPair Condition { get; set; } = new()
    {
        ConditionIndex = -1,
        IsTrue = false
    };
    public eWardenObjectiveEventTrigger Trigger { get; set; } = eWardenObjectiveEventTrigger.None;
    public uint ChainPuzzle { get; set; } = 0u;
    public bool UseStaticBioscanPoints { get; set; } = false;

    // General Fields
    public eDimensionIndex DimensionIndex { get; set; } = eDimensionIndex.Reality;
    public LG_LayerType Layer { get; set; } = LG_LayerType.MainLayer;    
    public eLocalZoneIndex LocalIndex { get; set; } = eLocalZoneIndex.Zone_0;
    public Vector3 Position { get; set; } = Vector3.zero;
    public float Delay { get; set; } = 0.0f;
    public float Duration { get; set; } = 0.0f;
    public LocaleText WardenIntel { get; set; } = LocaleText.Empty;
    public uint SoundID { get; set; } = 0u;
    public LocaleText SoundSubtitle { get; set; } = LocaleText.Empty;
    public uint DialogueID { get; set; } = 0u;
    public int Count { get; set; } = 0;
    public bool Enabled { get; set; } = true; // different default value than vanilla!
    public bool SpecialBool { get; set; } = false;
    public int SpecialNumber { get; set; } = -1;
    public LocaleText SpecialText { get; set; } = LocaleText.Empty;
    public string WorldEventObjectFilter { get => SpecialText; set => SpecialText = new(value); }

    // Common Updater
    public WEE_SubObjectiveData SubObjective { get; set; } = new();
    public WEE_UpdateFogData Fog { get; set; } = new();

    // Command Specific
    public bool CleanUpEnemiesBehind { get; set; } = true;
    public WEE_ReactorEventData Reactor { get; set; } = new();
    public WEE_CountdownData Countdown { get; set; } = new();
    public WEE_ZoneLightData SetZoneLight { get; set; } = new();
    public WEE_CleanupEnemiesData CleanupEnemies { get; set; } = new();
    public WEE_SpawnHibernateData SpawnHibernates { get; set; } = new();
    public WEE_SpawnScoutData SpawnScouts { get; set; } = new();

    // Hirnu
    public WEE_AddTerminalCommand AddTerminalCommand { get; set; } = new();
    public WEE_AddTerminalCommand AddCommand { get => AddTerminalCommand; set => AddTerminalCommand = value; }
    public WEE_HideTerminalCommand HideTerminalCommand { get; set; } = new();
    public WEE_HideTerminalCommand HideCommand { get => HideTerminalCommand; set => HideTerminalCommand = value; }
    public WEE_UnhideTerminalCommand UnhideTerminalCommand { get; set; } = new();
    public WEE_UnhideTerminalCommand UnhideCommand { get => UnhideTerminalCommand; set => UnhideTerminalCommand = value; }

    // Amor
    public WEE_NestedEvent NestedEvent { get; set; } = new();
    public WEE_StartEventLoop StartEventLoop { get; set; } = new();
    public WEE_StartEventLoop EventLoop { get => StartEventLoop; set => StartEventLoop = value; }
    public WEE_TeleportPlayer TeleportPlayer { get; set; } = new();
    public WEE_InfectPlayer InfectPlayer { get; set; } = new();
    public WEE_DamagePlayer DamagePlayer { get; set; } = new();
    public WEE_RevivePlayer RevivePlayer { get; set; } = new();
    public WEE_AdjustTimer AdjustTimer { get; set; } = new();
    public WEE_CountupData Countup { get; set; } = new();
    public WEE_NavMarkerData NavMarker { get; set; } = new();
    public WEE_ShakeScreen CameraShake { get; set; } = new();
    public WEE_StartPortalMachine Portal { get; set; } = new();
    public WEE_SetSuccessScreen SuccessScreen { get; set; } = new();
    public List<WEE_SubObjectiveData> MultiProgression { get; set; } = new();
    public WEE_PlayWaveDistantRoar WaveRoarSound { get; set; } = new();
    public WEE_CustomHudText CustomHudText { get; set; } = new();
    public WEE_CustomHudText CustomHud { get => CustomHudText; set => CustomHudText = value; }
    public WEE_SpecialHudTimer SpecialHudTimer { get; set; } = new();
    public WEE_SpecialHudTimer SpecialHud { get => SpecialHudTimer; set => SpecialHudTimer = value; }
    public WEE_ForcePlayerDialogue PlayerDialogue { get; set; } = new();
    public WEE_SetTerminalLog SetTerminalLog { get; set; } = new();
    public WEE_SetTerminalLog TerminalLog { get => SetTerminalLog; set => SetTerminalLog = value; }
    public List<WEE_SetPocketItem> ObjectiveItems { get; set; } = new();
}

#region OG_EVENTS
public sealed class WEE_SubObjectiveData
{
    public bool DoUpdate { get; set; } = false;
    public LocaleText CustomSubObjectiveHeader { get; set; } = LocaleText.Empty;
    public LocaleText CustomSubObjective { get; set; } = LocaleText.Empty;
    public uint Index { get; set; } = 0u;
    public int Priority { get; set; } = 1;
    public LG_LayerType Layer { get; set; } = LG_LayerType.MainLayer;
    public bool IsLayerIndependent { get; set; } = true;
    public LocaleText OverrideTag { get; set; } = LocaleText.Empty;
}

public sealed class WEE_UpdateFogData
{
    public bool DoUpdate { get; set; } = false;
    public uint FogSetting { get; set; } = 0u;
    public float FogTransitionDuration { get; set; } = 0.0f;
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
        Verify,
        Idle
    }
}

public sealed class WEE_CountdownData
{
    public float Duration { get; set; } = 0.0f;
    public bool CanShowHours { get; set; } = true;
    public LocaleText TimerText { get; set; } = LocaleText.Empty;
    public LocaleText TitleText { get => TimerText; set => TimerText = value; }
    public Color TimerColor { get; set; } = Color.red;
    public List<EventsOnTimerProgress> EventsOnProgress { get; set; } = new();
    public List<WardenObjectiveEventData> EventsOnDone { get; set; } = new();
}

public sealed class WEE_CleanupEnemiesData
{
    public CleanUpType Type { get; set; } = CleanUpType.Despawn;
    public int AreaIndex { get; set; } = -1;
    public bool IncludeHibernate { get; set; } = true;
    public bool IncludeAggressive { get; set; } = true;
    public bool IncludeScout { get; set; } = true;
    public uint[] ExcludeEnemyID { get; set; } = Array.Empty<uint>();
    public uint[] IncludeOnlyID { get; set; } = Array.Empty<uint>();

    public void DoClear(AIG_CourseNode node)
    {
        if (!SNet.IsMaster || node == null || node.m_enemiesInNode == null)
            return;

        foreach (EnemyAgent enemy in node.m_enemiesInNode.ToArray())
        {
            bool clear = enemy.AI.Mode switch
            {
                AgentMode.Agressive => IncludeAggressive,
                AgentMode.Scout => IncludeScout,
                AgentMode.Hibernate => IncludeHibernate,
                _ => true
            };

            if (!clear || ExcludeEnemyID.Contains(enemy.EnemyDataID)) continue;
            else if (IncludeOnlyID.Length > 0 && !IncludeOnlyID.Contains(enemy.EnemyDataID)) continue;

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

    public enum CleanUpType
    {
        Kill,
        Despawn
    }
}

public sealed class WEE_ZoneLightData
{
    public ModifierType Type { get; set; } = ModifierType.RevertToOriginal;
    public uint LightDataID { get; set; } = 0u;
    public float TransitionDuration { get; set; } = 0.5f;
    public int Seed { get; set; } = 0;
    public bool UseRandomSeed => Seed == 0;
    public enum ModifierType : byte
    {
        RevertToOriginal,
        SetZoneLightData
    }
}

public sealed class WEE_SpawnHibernateData
{
    public int AreaIndex { get; set; } = -1; 
    public int[] AreaBlacklist { get; set; } = Array.Empty<int>();
    public uint EnemyID { get; set; } = 0u;
    public int Count { get; set; } = 0;
    public Vector3 Position { get; set; } = Vector3.zero;
    public Vector3 Rotation { get; set; } = Vector3.zero;
}

public sealed class WEE_SpawnScoutData
{
    public int AreaIndex { get; set; } = -1;
    public int[] AreaBlacklist { get; set; } = Array.Empty<int>();
    public eEnemyGroupType GroupType { get; set; }
    public eEnemyRoleDifficulty Difficulty { get; set; }
    public int Count { get; set; } = 0;
}
#endregion

#region HIRNU_EVENTS
public sealed class WEE_AddTerminalCommand
{
    public int TerminalIndex { get; set; } = 0;
    public int CommandNumber { get; set; } = 6;
    public string Command { get; set; } = string.Empty;
    public LocaleText CommandDesc { get; set; } = LocaleText.Empty;
    public bool AutoIndentCommandDesc { get; set; } = false;
    public List<TerminalOutput> PostCommandOutputs { get; set; } = new();
    public List<WardenObjectiveEventData> CommandEvents { get; set; } = new();
    public bool ProgressWaitBeforeEvents { get; set; } = false;
    public TERM_CommandRule SpecialCommandRule { get; set; } = TERM_CommandRule.Normal;
}

public sealed class WEE_HideTerminalCommand
{
    public int TerminalIndex { get; set; } = 0; 
    public TERM_Command CommandEnum { get; set; } = TERM_Command.None;
    public int CommandNumber { get; set; } = 0;
    public bool DeleteCommand { get; set; } = false;
}

public sealed class WEE_UnhideTerminalCommand
{
    public int TerminalIndex { get; set; } = 0; 
    public TERM_Command CommandEnum { get; set; } = TERM_Command.None;
    public int CommandNumber { get; set; } = 0;
}
#endregion

#region AMOR_EVENTS
public sealed class WEE_NestedEvent
{
    public NestedMode Type { get; set; } = NestedMode.ActivateAll;
    public int MaxRandomEvents { get; set; } = -1;
    public bool AllowRepeatsInRandom { get; set; } = false;
    public List<WardenObjectiveEventData> EventsToActivate { get; set; } = new();
    public List<EventsOnRandomWeight> WheelOfEvents { get; set; } = new();
    public enum NestedMode : byte
    {
        ActivateAll,
        RandomAny,
        RandomWeighted
    }
    public struct EventsOnRandomWeight
    {
        public string DebugName { get; set; }
        public float Weight { get; set; }
        public int RepeatCount { get; set; }
        public bool IsInfinite { get; set; }
        public List<WardenObjectiveEventData> Events { get; set; }
    }
}

public sealed class WEE_StartEventLoop
{
    public int LoopIndex { get; set; } = 0;
    public float LoopDelay { get; set; } = 1.0f;
    public int LoopCount { get; set; } = -1;
    public List<WardenObjectiveEventData> EventsToActivate { get; set; } = new();
}

public enum PlayerIndex : byte
{
    P0,
    P1,
    P2,
    P3
}

public sealed partial class WEE_TeleportPlayer // old
{
    public HashSet<PlayerIndex> PlayerFilter { get; set; } = new();
    public bool PlayWarpAnimation { get; set; } = true;
    public bool SendBPUsToHost { get; set; } = false;
    public Vector3 Player0Position { get; set; } = Vector3.zero;
    public int P0LookDir { get; set; } = 0;
    public Vector3 Player1Position { get; set; } = Vector3.zero;
    public int P1LookDir { get; set; } = 0;
    public Vector3 Player2Position { get; set; } = Vector3.zero;
    public int P2LookDir { get; set; } = 0;
    public Vector3 Player3Position { get; set; } = Vector3.zero;
    public int P3LookDir { get; set; } = 0;
}

public sealed partial class WEE_TeleportPlayer // new
{
    public bool FlashTeleport { get; set; } = false;
    public bool WarpSentries { get; set; } = true;
    public bool WarpBigPickups { get; set; } = true;
    public bool SendBigPickupsToHost { get => SendBPUsToHost; set => SendBPUsToHost = value; }
    public List<TeleportData> TPData { get; set; } = new();
    public struct TeleportData
    { 
        public PlayerIndex PlayerIndex { get; set; }
        [JsonPropertyName("DimensionIndex")]
        public eDimensionIndex Dimension { get; set; }
        public Vector3 Position { get; set; }
        public string WorldEventObjectFilter { get; set; }
        [JsonPropertyName("LookDirection")]
        public int LookDir { get; set; }
        [JsonPropertyName("LookDirectionV3")]
        public Vector3 LookDirV3 { get; set; }
        public bool PlayWarpAnimation { get; set; }
        [JsonPropertyName("FlashDuration")]
        public float Duration { get; set; }

        [JsonIgnore]
        public PlayerAgent Player { get; set; }
        [JsonIgnore]
        public eDimensionIndex LastDimension { get; set; }
        [JsonIgnore]
        public Vector3 LastPosition { get; set; }
        [JsonIgnore]
        public Vector3 LastLookDirV3 { get; set; }
        [JsonIgnore]
        public List<IWarpableObject> ItemsToWarp { get; set; }
    }
}

public sealed class WEE_InfectPlayer
{
    public HashSet<PlayerIndex> PlayerFilter { get; set; } = new() { PlayerIndex.P0, PlayerIndex.P1, PlayerIndex.P2, PlayerIndex.P3};
    public float InfectionAmount { get; set; } = 0.0f;
    public bool InfectOverTime { get; set; } = false;
    public float Interval { get; set; } = 1.0f;
    public bool UseZone { get; set; } = false;
}

public sealed class WEE_DamagePlayer
{
    public HashSet<PlayerIndex> PlayerFilter { get; set; } = new() { PlayerIndex.P0, PlayerIndex.P1, PlayerIndex.P2, PlayerIndex.P3 };
    public float DamageAmount { get; set; } = 0.0f;
    public bool DamageOverTime { get; set; } = false;
    public float Interval { get; set; } = 1.0f;
    public bool UseZone { get; set; } = false;
}

public sealed class WEE_RevivePlayer
{
    public HashSet<PlayerIndex> PlayerFilter { get; set; } = new() { PlayerIndex.P0, PlayerIndex.P1, PlayerIndex.P2, PlayerIndex.P3 };
}

public sealed class WEE_AdjustTimer
{
    public float Duration { get; set; } = 0.0f;
    public float Speed { get; set; } = 0.0f;
    public bool UpdateTitleText { get; set; } = false;
    public LocaleText TitleText { get; set; } = LocaleText.Empty;
    public bool UpdateText { get; set; } = false;
    public bool UpdateBodyText { get => UpdateText; set => UpdateText = value; }
    public LocaleText CustomText { get; set; } = LocaleText.Empty;
    public LocaleText BodyText { get => CustomText; set => CustomText = value; }
    public bool UpdateColor { get; set; } = false; 
    public Color TimerColor { get; set; } = Color.red;
}

public sealed class WEE_CountupData
{
    public float Duration { get; set; } = 0.0f;
    public float StartValue { get; set; } = 0.0f;
    public float Speed { get; set; } = 1.0f;
    public LocaleText TimerText { get; set; } = LocaleText.Empty;
    public LocaleText TitleText { get => TimerText; set => TimerText = value; }
    public LocaleText CustomText { get; set; } = LocaleText.Empty;
    public LocaleText BodyText { get => CustomText; set => CustomText = value; }
    public Color TimerColor { get; set; } = Color.red;
    public int DecimalPoints { get; set; } = 0;
    public List<EventsOnTimerProgress> EventsOnProgress { get; set; } = new();
    public List<WardenObjectiveEventData> EventsOnDone { get; set; } = new();
}

public struct EventsOnTimerProgress
{
    public float Progress { get; set; }
    public List<WardenObjectiveEventData> Events { get; set; }
}

public sealed class WEE_NavMarkerData
{
    public int Index { get; set; } = 0;
    public NavMarkerOption Style { get; set; } = NavMarkerOption.Waypoint | NavMarkerOption.Distance;
    public LocaleText Title { get; set; } = LocaleText.Empty;
    public Color Color { get; set; } = new(0.701f, 0.435f, 0.964f, 1.0f);
    public bool UsePin { get; set; } = true;
}

public sealed class WEE_ShakeScreen
{
    public float Radius { get; set; } = 0.0f;
    public float Duration { get; set; } = 0.0f;
    public float Amplitude { get; set; } = 0.0f;
    public float Frequency { get; set; } = 0.0f;
    public bool Directional { get; set; } = true;
}

public sealed class WEE_StartPortalMachine
{
    public eDimensionIndex TargetDimension { get; set; } = eDimensionIndex.Dimension_1;
    public float TeleportDelay { get; set; } = 5.0f;
    public bool PreventPortalWarpTeamEvent { get; set; } = false;
}

public sealed class WEE_SetSuccessScreen
{
    public ScreenType Type { get; set; } = ScreenType.SetSuccessScreen;
    public WinScreen CustomSuccessScreen { get; set; } = WinScreen.Empty;
    public eCM_MenuPage FakeEndScreen { get; set; } = eCM_MenuPage.CMP_EXPEDITION_SUCCESS;
    public enum ScreenType : byte
    {
        SetSuccessScreen,
        FlashFakeScreen
    }
}

public sealed class WEE_PlayWaveDistantRoar
{
    public WaveRoarSound RoarSound { get; set; } = WaveRoarSound.Striker;
    public WaveRoarSize RoarSize { get; set; } = WaveRoarSize.Small;
    public bool IsOutside { get; set; } = false;
    public enum WaveRoarSound : byte
    {
        Striker,
        Shooter,
        Birther,
        Shadow,
        Tank,
        Flyer,
        Immortal,
        Bullrush,
        Pouncer,
        Striker_Berserk,
        Shooter_Spread
    }
    public enum WaveRoarSize : byte
    {
        Small,
        Medium,
        Big
    }
}

public sealed class WEE_CustomHudText
{
    public LocaleText Title { get; set; } = LocaleText.Empty;
    public LocaleText TitleText { get => Title; set => Title = value; } 
    public LocaleText Body { get; set; } = LocaleText.Empty;
    public LocaleText BodyText { get => Body; set => Body = value; }
}

public sealed class WEE_SpecialHudTimer
{
    public float Duration { get; set; } = 0.0f;
    public SpecialHudType Type { get; set; } = SpecialHudType.StartTimer;
    public int Index { get; set; } = 0;
    public LocaleText Message { get; set; } = LocaleText.Empty;
    public ePUIMessageStyle Style { get; set; } = ePUIMessageStyle.Default;
    public int Priority { get; set; } = -2;
    public bool ShowTimeInProgressBar { get; set; } = true;    
    public List<EventsOnTimerProgress> EventsOnProgress { get; set; } = new();
    public List<WardenObjectiveEventData> EventsOnDone { get; set; } = new();
    public enum SpecialHudType : byte
    {
        StartTimer,
        StartIndexTimer,
        StartPersistent,
        StopIndex,
        StopAll
    }
}

public sealed class WEE_ForcePlayerDialogue
{
    public DialogueType Type { get; set; } = DialogueType.Closest;
    public PlayerIndex CharacterID { get; set; } = PlayerIndex.P0;
    public PlayerIntensityState IntensityState { get; set; } = PlayerIntensityState.Exploration;
    public enum DialogueType : byte
    {
        Closest,
        Specific, 
        Random
    }
    public enum PlayerIntensityState : byte
    {
        Exploration, 
        Stealth,
        Encounter,
        Combat
    }
}

public sealed class WEE_SetTerminalLog
{
    public int TerminalIndex { get; set; } = 0;
    public LogEventType Type { get; set; } = LogEventType.Add;
    public string FileName { get; set; } = string.Empty;    
    public LocaleText FileContent { get; set; } = LocaleText.Empty;
    public Language FileContentOriginalLanguage { get; set; } = Language.English;    
    public uint AttachedAudioFile { get; set; } = 0u;
    public int AttachedAudioByteSize { get; set; } = 0;
    public uint PlayerDialogToTriggerAfterAudio { get; set; } = 0u;
    public List<WardenObjectiveEventData> EventsOnFileRead { get; set; } = new();
    public enum LogEventType : byte
    {
        Add,
        Remove
    }
}

public sealed class WEE_SetPocketItem
{
    public int Index { get; set; } = 0;
    public int Count { get; set; } = 0;
    public bool IsOnTop { get; set; } = false;
    public LocaleText ItemName { get; set; } = LocaleText.Empty;
    public PlayerTagType TagType { get; set; } = PlayerTagType.Custom;
    public PlayerIndex PlayerIndex { get; set; } = PlayerIndex.P0;
    public string CustomTag { get; set; } = string.Empty;

    [JsonIgnore]
    public string? Tag { get; set; } = string.Empty;
    public bool ShouldRemove => Count < 1;
    private string LiveCount => Count > 1 ? $"{Count} " : string.Empty;

    public string FormatString()
    {
        return $"{LiveCount}{ItemName} <uppercase><color=#ffffff{MathUtil.ZeroOneRangeToHex(0.2f)}>[{Tag}]</color></uppercase>";
    }

    public enum PlayerTagType : byte
    {
        Custom,
        Specific,
        Random,
        Closest
    }
}
#endregion
