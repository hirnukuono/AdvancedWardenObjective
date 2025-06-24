using ChainedPuzzles;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal class SolveSecurityDoorAlarmEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SolveSecurityDoorAlarm;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) return;

        var status = door.LastStatus;

        if (status == eDoorStatus.Closed_LockedWithChainedPuzzle || status == eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm)
        {
            var state = door.m_locks.ChainedPuzzleToSolve.m_stateReplicator.State;
            state.status = eChainedPuzzleStatus.Solved;
            state.isSolved = true;
            state.isActive = false;
            if (IsMaster) door.m_locks.ChainedPuzzleToSolve.m_stateReplicator.State = state;
            var doorstate = door.m_sync.GetCurrentSyncState();
            doorstate.status = eDoorStatus.Unlocked;
            door.m_sync.SetStateUnsynced(doorstate);
        }
    }
}
