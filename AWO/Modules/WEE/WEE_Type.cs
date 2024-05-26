namespace AWO.Modules.WEE;

public enum WEE_Type
{
    CloseSecurityDoor = WEE_EnumInjector.ExtendedIndex + 0,
    LockSecurityDoor, // TODO: add door interaction text
    SetDoorInteraction, // ??
    TriggerSecurityDoorAlarm, 
    SolveSecurityDoorAlarm, // Only solves "displayed" door alarm, not active CP
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
    SolveSingleObjectiveItem, // Deprecated
    SetLightDataInZone, // TODO: Partially done, need to work on animation
    AlertEnemiesInZone, 
    CleanupEnemiesInZone, // Kill, Despawn has merged with this
    SpawnHibernateInZone, // Added by Inas
    SpawnScoutInZone, // Added by Inas
    SaveCheckpoint,
    MoveExtractionWorldPosition, 
    SetBlackoutEnabled,

    // Hirnu AWO Events:
    AddTerminalCommand,
    HideTerminalCommand,
    UnhideTerminalCommand,
    AddChainPuzzleToSecurityDoor,

    // Amor AWO Events:
    NestedEvent = WEE_EnumInjector.ExtendedIndex + 10000,
    StartEventLoop = WEE_EnumInjector.ExtendedIndex + 10001,
    StopEventLoop = WEE_EnumInjector.ExtendedIndex + 10002,
    TeleportPlayer = WEE_EnumInjector.ExtendedIndex + 10003,
    InfectPlayer = WEE_EnumInjector.ExtendedIndex + 10004,
    DamagePlayer = WEE_EnumInjector.ExtendedIndex + 10005,
    RevivePlayer = WEE_EnumInjector.ExtendedIndex + 10006
}