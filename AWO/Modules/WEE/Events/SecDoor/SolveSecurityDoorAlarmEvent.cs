using AK;
using AWO.Modules.WEE;
using ChainedPuzzles;
using Enemies;
using GameData;
using LevelGeneration;
using SNetwork;
using System.Collections;
using UnityEngine;

namespace AWO.WEE.Events.SecDoor;

internal class SolveSecurityDoorAlarmEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SolveSecurityDoorAlarm;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone))
        {
            LogError("Cannot find zone!");
            return;
        }

        if (!TryGetZoneEntranceSecDoor(zone, out var door))
        {
            LogError("Cannot find security door!");
            return;
        }
        var status = door.LastStatus;

        if (status == eDoorStatus.Open || status == eDoorStatus.Unlocked || status == eDoorStatus.Opening) return;

        if (status != eDoorStatus.Closed_LockedWithChainedPuzzle || status != eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm)
        {
            var state = door.m_locks.ChainedPuzzleToSolve.m_stateReplicator.State;
            state.status = eChainedPuzzleStatus.Solved;
            state.isSolved = true;
            state.isActive = false;
            if (IsMaster) door.m_locks.ChainedPuzzleToSolve.m_stateReplicator.State = state;
            var doorstate = door.m_sync.GetCurrentSyncState();
            doorstate.status = eDoorStatus.Unlocked;
            door.m_sync.SetStateUnsynced(doorstate);
            return;
        }
    }
}
