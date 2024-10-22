namespace AWO.Modules.WEE;

public enum WEE_Type
{
    CloseSecurityDoor = WEE_EnumInjector.ExtendedIndex + 0,
    LockSecurityDoor,
    SetDoorInteraction, // Missing
    TriggerSecurityDoorAlarm, 
    SolveSecurityDoorAlarm,
    StartReactor, 
    ModifyReactorWaveState, 
    ForceCompleteReactor, 
    ForceCompleteLevel, 
    ForceFailLevel, 
    Countdown, 
    SetLevelFailCheckEnabled, 
    SetLevelFailWhenAnyPlayerDowned, 
    KillAllPlayers, 
    KillPlayersInZone, 
    SolveSingleObjectiveItem, // ?
    SetLightDataInZone, // TODO: Partially done, need to work on animation
    AlertEnemiesInZone, 
    CleanupEnemiesInZone, // Kill, Despawn has merged with this
    SpawnHibernateInZone, // Added by Inas
    SpawnScoutInZone, // Added by Inas
    SaveCheckpoint,
    MoveExtractionWorldPosition, 
    SetBlackoutEnabled,

    // Hirnu AWO EventSyncState:
    AddTerminalCommand,
    HideTerminalCommand,
    UnhideTerminalCommand,
    AddChainPuzzleToSecurityDoor,

    // Amor AWO EventSyncState:
    NestedEvent = WEE_EnumInjector.ExtendedIndex + 10000,
    StartEventLoop = WEE_EnumInjector.ExtendedIndex + 10001,
    StopEventLoop = WEE_EnumInjector.ExtendedIndex + 10002,
    TeleportPlayer = WEE_EnumInjector.ExtendedIndex + 10003,
    InfectPlayer = WEE_EnumInjector.ExtendedIndex + 10004,
    DamagePlayer = WEE_EnumInjector.ExtendedIndex + 10005,
    RevivePlayer = WEE_EnumInjector.ExtendedIndex + 10006,
    AdjustAWOTimer = WEE_EnumInjector.ExtendedIndex + 10007,
    Countup = WEE_EnumInjector.ExtendedIndex + 10008,
    ForceCompleteChainPuzzle = WEE_EnumInjector.ExtendedIndex + 10009,
    SpawnNavMarker = WEE_EnumInjector.ExtendedIndex + 10010,
    ShakeScreen = WEE_EnumInjector.ExtendedIndex + 10011,
    StartPortalMachine = WEE_EnumInjector.ExtendedIndex + 10012,
    SetSuccessScreen = WEE_EnumInjector.ExtendedIndex + 10013,
    PlaySubtitles = WEE_EnumInjector.ExtendedIndex + 10014,
    MultiProgression = WEE_EnumInjector.ExtendedIndex + 10015,
    PlayWaveRoarSound = WEE_EnumInjector.ExtendedIndex + 10016,
    CustomHudText = WEE_EnumInjector.ExtendedIndex + 10017
}