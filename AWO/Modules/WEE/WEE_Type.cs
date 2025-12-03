namespace AWO.Modules.WEE;

public enum WEE_Type
{
    CloseSecurityDoor = WEE_EnumInjector.ExtendedIndex + 0,
    LockSecurityDoor,
    SetDoorInteraction, 
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
    SolveSingleObjectiveItem, // Obsolete
    SetLightDataInZone,
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

    // Dinorush AWO Events:
    SetActiveEnemyWave,

    // Amor AWO Events:
    NestedEvent = WEE_EnumInjector.ExtendedIndex + 10000,
    StartEventLoop,
    StopEventLoop,
    TeleportPlayer,
    InfectPlayer,
    DamagePlayer,
    RevivePlayer,
    AdjustAWOTimer,
    Countup,
    ForceCompleteChainPuzzle,
    SpawnNavMarker,
    ShakeScreen,
    StartPortalMachine,
    SetSuccessScreen,
    PlaySubtitles,
    MultiProgression,
    PlayWaveRoarSound,
    CustomHudText,
    SpecialHudTimer,
    ForcePlayPlayerDialogue,
    SetTerminalLog,
    SetPocketItem,
    DoInteractWeakDoorsInZone,
    ToggleInteractWeakDoorsInZone,
    PickupSentries,
    SetOutsideDimensionData
}