using AK;
using ChainedPuzzles;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal class ResetSecurityDoorAlarmEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ResetSecurityDoorAlarm;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door))
            return;

        var doorState = door.m_sync.GetCurrentSyncState();
        switch (doorState.status)
        {
            case eDoorStatus.ChainedPuzzleActivated:
                if (e.Enabled)
                    return;
                break;
            case eDoorStatus.Unlocked:
                break;
            default:
                return;
        }
        
        var anim = door.m_anim.TryCast<LG_SecurityDoor_Anim>();
        if (anim == null)
            return;

        anim.EnableAnimator();
        anim.m_animator.Play("ClosedIdle");
        anim.DelayedDisableAnimator();
    }

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) 
            return;

        var puzzle = door.m_locks.ChainedPuzzleToSolve;
        if (puzzle == null)
        {
            LogError($"Unable to reset alarm on a security door that doesn't have one!");
            return;
        }

        var doorState = door.m_sync.GetCurrentSyncState();
        var status = doorState.status;

        ResetPuzzleCores(puzzle);

        if (status == eDoorStatus.ChainedPuzzleActivated)
        {
            if (e.Enabled)
            {
                puzzle.m_chainedPuzzleCores[0].Activate();
                return;
            }
            else if (puzzle.Data.DisableSurvivalWaveOnComplete && Mastermind.Current.TryGetEvent(puzzle.GetSetSurvivalWaveID(), out var masterMindEvent))
            {
                masterMindEvent.StopEvent();
                puzzle.SetSurvivalWaveID(0);
            }
        }

        var puzzleState = puzzle.m_stateReplicator.State;
        // Need to reset isSolved so not using Deactivate interact.
        puzzleState.isSolved = false;
        puzzleState.isActive = false;
        puzzleState.status = eChainedPuzzleStatus.Disabled;
        puzzle.m_stateReplicator.State = puzzleState;

        if (status != eDoorStatus.Unlocked && status != eDoorStatus.ChainedPuzzleActivated)
            return;

        doorState.status = puzzle.Data.TriggerAlarmOnActivate ? eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm : eDoorStatus.Closed_LockedWithChainedPuzzle;
        var sync = door.m_sync.Cast<LG_Door_Sync>();
        sync.m_stateReplicator.State = doorState;
    }

    static void ResetPuzzleCores(ChainedPuzzleInstance puzzleInstance)
    {
        var cores = puzzleInstance.m_chainedPuzzleCores;
        for (int i = 0; i < cores.Length; i++)
            ResetCore(cores[i]);
    }

    static void ResetCore(iChainedPuzzleCore core)
    {
        var clusterCore = core.TryCast<CP_Cluster_Core>();
        if (clusterCore != null)
        {
            var clusterSync = clusterCore.m_sync.Cast<CP_Cluster_Sync>();
            switch (clusterSync.GetCurrentState().status)
            {
                case eClusterStatus.SplineReveal:
                    clusterCore.m_spline?.Cast<CP_Holopath_Spline>().m_sound.Post(EVENTS.BIOSCAN_TUBE_EMITTER_STOP);
                    break;
                default:
                    break;
            }

            // Technically setting the Cluster parent to Disabled also sets the children, so there's a bunch of redundancies,
            // but some fields need to be manually set and I'm too lazy to modify the copied structure from CompleteChainPuzzle
            foreach (var child in clusterCore.m_childCores)
                ResetCore(child);

            if (clusterSync.m_syncRoutine != null)
            {
                clusterSync.StopCoroutine(clusterSync.m_syncRoutine);
                clusterSync.m_syncRoutine = null;
            }

            var state = clusterSync.m_latestState;
            state.status = eClusterStatus.Disabled;
            state.progress = 0f;
            clusterSync.m_latestState = state;
            clusterSync.m_stateReplicator.State = state;
        }
        else
        {
            var bioCore = core.Cast<CP_Bioscan_Core>();
            var bioSync = bioCore.m_sync.Cast<CP_Bioscan_Sync>();
            switch (bioSync.GetCurrentState().status)
            {
                case eBioscanStatus.SplineReveal:
                    bioCore.m_spline?.Cast<CP_Holopath_Spline>().m_sound.Post(EVENTS.BIOSCAN_TUBE_EMITTER_STOP);
                    break;
                default:
                    break;
            }

            if (bioCore.IsMovable && !bioCore.m_movingComp.OnlyMoveWhenScannig)
                bioCore.m_movingComp.StopMoving();

            if (bioSync.m_syncRoutine != null)
            {
                bioSync.StopCoroutine(bioSync.m_syncRoutine);
                bioSync.m_syncRoutine = null;
            }

            var scanner = bioCore.m_playerScanner.Cast<CP_PlayerScanner>();
            scanner.m_scanProgression = scanner.m_scanProgressionLast = 0;

            var state = bioSync.m_latestState;
            state.status = eBioscanStatus.Disabled;
            state.progress = 0f;
            bioSync.m_latestState = state;
            bioSync.m_stateReplicator.State = state;
        }
    }
}
