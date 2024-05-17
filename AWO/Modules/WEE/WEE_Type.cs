namespace AWO.Modules.WEE;

public enum WEE_Type
{
    CloseSecurityDoor //Done
        = WEE_EnumInjector.ExtendedIndex + 0,

    LockSecurityDoor, //Done
    SetDoorInteraction,
    TriggerSecurityDoorAlarm, //Done
    SolveSecurityDoorAlarm, //TODO: Partially works
    StartReactor, //Done
    ModifyReactorWaveState, //Done
    ForceCompleteReactor, //Done
    ForceCompleteLevel, //Done
    ForceFailLevel, //Done
    Countdown, //Done
    SetLevelFailCheckEnabled, //Done
    SetLevelFailWhenAnyPlayerDowned, 
    KillAllPlayers, //Done
    KillPlayersInZone, //Done
    SolveSingleObjectiveItem, //Done
    SetLightDataInZone, //TODO: Partially Done, Need to Work on Animation
    AlertEnemiesInZone, //Done
    CleanupEnemiesInZone, //Done, Kill, Despawn Has Merged with this
    SpawnHibernateInZone, // Added by Inas
    SpawnScoutInZone, // Added by Inas
    SaveCheckpoint, //Done
    MoveExtractionWorldPosition, //Done
    SetBlackoutEnabled, //Done
    AddTerminalCommand,
    HideTerminalCommand,
    UnhideTerminalCommand,
    AddChainPuzzleToSecurityDoor
}
