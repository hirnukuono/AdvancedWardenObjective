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
            LogError("Cannot find Security Door!");
            return;
        }
        var status = door.LastStatus;

        if (status == eDoorStatus.Open || status == eDoorStatus.Unlocked || status == eDoorStatus.Opening) return;

        if (status != eDoorStatus.Closed_LockedWithChainedPuzzle || status != eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm)
        {
            //ChainedPuzzleInstance uusi = new() { Data = ChainedPuzzleDataBlock.GetBlock(4)}

            //door.SetupChainedPuzzleLock(0);
            var state = door.m_locks.ChainedPuzzleToSolve.m_stateReplicator.State;
            state.status = eChainedPuzzleStatus.Solved;
            state.isSolved = true;
            state.isActive = false;
            if (SNet.IsMaster) door.m_locks.ChainedPuzzleToSolve.m_stateReplicator.State = state;
            var doorstate = door.m_sync.GetCurrentSyncState();
            doorstate.status = eDoorStatus.Unlocked;
            door.m_sync.SetStateUnsynced(doorstate);
            return;
        }

        var cp = door.m_locks.ChainedPuzzleToSolve;

        if (cp != null && !cp.IsSolved)
        {
            //TODO: 씨발 개힘듬
            RecursivelySolveAlarm(cp.TryCast<iChainedPuzzleOwner>());
        }
    }

    protected void RecursivelySolveAlarm(iChainedPuzzleOwner cpOwner)
    {
        var count = cpOwner.NRofPuzzles();
        for (int i = 0; i < count; i++)
        {
            var cp = cpOwner.GetPuzzle(i);
            var internalOwner = cp.TryCast<iChainedPuzzleOwner>();
            if (internalOwner != null)
            {
                RecursivelySolveAlarm(cpOwner);
            }

            var clusterCore = cp.TryCast<CP_Cluster_Core>();
            if (clusterCore != null)
            {
                if (IsMaster)
                {
                    clusterCore.m_sync.SetStateData(eClusterStatus.Finished, 1.0f);
                }
                continue;
            }

            var basicCore = cp.TryCast<CP_Bioscan_Core>();
            if (basicCore != null)
            {
                CoroutineManager.StartCoroutine(SolveBasicCore(basicCore).WrapToIl2Cpp());
                continue;
            }

            var hackCore = cp.TryCast<CP_Hack_Core>();
            if (hackCore != null)
            {
                if (IsMaster)
                {
                    hackCore.m_stateSync.SetStatus(eStatus.Finished);
                }
                continue;
            }
        }
    }

    static IEnumerator SolveBasicCore(CP_Bioscan_Core basicCore)
    {
        if (basicCore == null)
            yield break;

        //basicCore.m_playerScanner.StartScan();
        basicCore.m_playerScanner.TryCast<MonoBehaviour>().gameObject.active = true;
        basicCore.m_spline.SetVisible(false);
        if (IsMaster)
        {
            basicCore.m_playerScanner.ResetScanProgression(1.0f);
            basicCore.m_sync.SetStateData(eBioscanStatus.Finished, 1.0f);

            yield return null;

            var spline = basicCore.m_spline.TryCast<CP_Holopath_Spline>();
            basicCore.m_sound.Post(EVENTS.BIOSCAN_PROGRESS_COUNTER_STOP, isGlobal: true);
            spline.m_sound.Post(EVENTS.BIOSCAN_TUBE_EMITTER_STOP, isGlobal: true); //STFU
            /*
            basicCore.Master_OnPlayerScanChangedCheckProgress(1.0f,
                new Il2CppSystem.Collections.Generic.List<Player.PlayerAgent>(),
                0,
                new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<bool>(0));
            */
        }
        yield return new WaitForSeconds(RNG.Float01 * 0.35f);

        CoroutineManager.BlinkOut(basicCore.gameObject);

        yield break;
    }
}
