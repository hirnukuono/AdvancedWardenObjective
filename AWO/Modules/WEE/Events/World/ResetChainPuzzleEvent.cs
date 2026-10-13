using AK;
using ChainedPuzzles;
using LevelGeneration;
using System.Diagnostics.CodeAnalysis;

namespace AWO.Modules.WEE.Events.World;

internal class ResetChainPuzzleEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ForceResetChainPuzzle;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerCommon(WEE_EventData e)
    {
        // If using ChainPuzzle version, don't do door logic
        uint chainPuzzle = e.SpecialNumber > 0 ? (uint)e.SpecialNumber : e.ChainPuzzle;
        ChainedPuzzleInstance? puzzleInstance;
        if (chainPuzzle != 0)
        {
            if (TryGetCPInstance(chainPuzzle, out puzzleInstance))
                FixChainedPuzzleSound(puzzleInstance);
            return;
        }

        if (!TryGetZoneEntranceSecDoor(e, out var door))
            return;

        puzzleInstance = door.m_locks.ChainedPuzzleToSolve;
        if (puzzleInstance == null)
            return;
        FixChainedPuzzleSound(puzzleInstance);

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
        uint chainPuzzle = e.SpecialNumber > 0 ? (uint)e.SpecialNumber : e.ChainPuzzle;
        if (chainPuzzle != 0)
        {
            if (TryGetCPInstance(chainPuzzle, out var puzzleInstance))
                ResetChainedPuzzle(e, puzzleInstance);
            else
                LogError($"A chained puzzle with index {chainPuzzle} was not found!");
        }
        else
            ResetDoor(e);
    }

    static void FixChainedPuzzleSound(ChainedPuzzleInstance puzzle)
    {
        // If DisableSurvivalWaveOnComplete is true, the sound is recycled when the puzzle is completed and needs to be re-registered.
        if (!puzzle.m_sound.m_registred)
            puzzle.m_sound = new(puzzle.m_parent.position);
    }

    static void ResetChainedPuzzle(WEE_EventData e, ChainedPuzzleInstance puzzle)
    {
        ResetPuzzleCores(puzzle);

        var puzzleState = puzzle.m_stateReplicator.State;
        if (puzzleState.isActive)
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
                puzzle.m_sound.Stop();
            }
        }

        // Need to reset isSolved so not using Deactivate interact.
        puzzleState.isSolved = false;
        puzzleState.isActive = false;
        puzzleState.status = eChainedPuzzleStatus.Disabled;
    }

    void ResetDoor(WEE_EventData e)
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
        
        ResetChainedPuzzle(e, puzzle);

        if (status != eDoorStatus.Unlocked && status != eDoorStatus.ChainedPuzzleActivated)
            return;

        doorState.status = puzzle.Data.TriggerAlarmOnActivate ? eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm : eDoorStatus.Closed_LockedWithChainedPuzzle;
        var sync = door.m_sync.Cast<LG_Door_Sync>();
        sync.m_stateReplicator.State = doorState;
    }

    private static bool TryGetCPInstance(uint ID, [NotNullWhen(true)] out ChainedPuzzleInstance? puzzleInstance)
    {
        foreach (var instance in ChainedPuzzleManager.Current.m_instances)
        {
            if (instance.Data.persistentID == ID)
            {
                puzzleInstance = instance;
                return true;
            }
        }
        puzzleInstance = null;
        return false;
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
