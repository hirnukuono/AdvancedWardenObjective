using ChainedPuzzles;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal class SolveSecurityDoorAlarmEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SolveSecurityDoorAlarm;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) return;

        var doorState = door.m_sync.GetCurrentSyncState();
        if (doorState.status == eDoorStatus.Closed_LockedWithChainedPuzzle || doorState.status == eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm)
        {
            var puzzleState = door.m_locks.ChainedPuzzleToSolve.m_stateReplicator.State;
            puzzleState.status = eChainedPuzzleStatus.Solved;
            puzzleState.isSolved = true;
            puzzleState.isActive = false;
            if (IsMaster) door.m_locks.ChainedPuzzleToSolve.m_stateReplicator.State = puzzleState;
            doorState.status = eDoorStatus.Unlocked;
            door.m_sync.SetStateUnsynced(doorState);
        }
    }
}
