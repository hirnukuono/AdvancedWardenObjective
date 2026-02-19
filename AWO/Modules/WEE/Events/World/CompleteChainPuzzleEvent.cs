using AK;
using BepInEx.Logging;
using ChainedPuzzles;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal class CompleteChainPuzzleEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ForceCompleteChainPuzzle;

    protected override void TriggerMaster(WEE_EventData e)
    {
        uint chainPuzzle = e.SpecialNumber > 0 ? (uint)e.SpecialNumber : e.ChainPuzzle; // resolve terminal field fallback
        if (!TryGetCPInstance(chainPuzzle, out var puzzleInstance) || !puzzleInstance.IsActive || puzzleInstance.IsSolved)
        {
            LogError($"An active chained puzzle with index {chainPuzzle} was not found!");
            return;
        }
        
        CoroutineManager.StartCoroutine(SolvePuzzleCores(puzzleInstance, e.Count).WrapToIl2Cpp());
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

    static IEnumerator SolvePuzzleCores(ChainedPuzzleInstance puzzleInstance, int count)
    {
        var cores = puzzleInstance.m_chainedPuzzleCores;
        for (int i = 0; i < cores.Length; i++)
        {
            if (i == count && count > 0) yield break;

            SolveCore(cores[i], i == cores.Length - 1);
        }
    }

    static void SolveCore(iChainedPuzzleCore core, bool finished = false)
    {
        var clusterCore = core.TryCast<CP_Cluster_Core>();
        if (clusterCore != null)
        {
            var clusterSync = clusterCore.m_sync.Cast<CP_Cluster_Sync>();
            switch (clusterSync.GetCurrentState().status)
            {
                case eClusterStatus.Finished:
                    return;
                case eClusterStatus.SplineReveal:
                    clusterCore.m_spline?.Cast<CP_Holopath_Spline>().m_sound.Post(EVENTS.BIOSCAN_TUBE_EMITTER_STOP);
                    break;
                default:
                    break;
            }

            foreach (var child in clusterCore.m_childCores)
                SolveCore(child);

            // Finished by last child; may have active coroutine
            if (clusterSync.m_syncRoutine != null)
            {
                clusterSync.StopCoroutine(clusterSync.m_syncRoutine);
                clusterSync.m_syncRoutine = null;
            }
            else if (clusterSync.GetCurrentState().status == eClusterStatus.Finished)
                return;

            var state = clusterSync.m_latestState;
            state.status = eClusterStatus.Finished;
            state.progress = 1f;
            clusterSync.m_latestState = state;
            clusterSync.m_stateReplicator.State = state;
        }
        else
        {
            var bioCore = core.Cast<CP_Bioscan_Core>();
            var bioSync = bioCore.m_sync.Cast<CP_Bioscan_Sync>();
            switch (bioSync.GetCurrentState().status)
            {
                case eBioscanStatus.Finished:
                    return;
                case eBioscanStatus.SplineReveal:
                    bioCore.m_spline?.Cast<CP_Holopath_Spline>().m_sound.Post(EVENTS.BIOSCAN_TUBE_EMITTER_STOP);
                    break;
                default:
                    break;
            }

            if (bioCore.IsMovable && !bioCore.m_movingComp.OnlyMoveWhenScannig)
                bioCore.m_movingComp.StopMoving();

            // May have active coroutine if forcibly finished right after normal finish
            if (bioSync.m_syncRoutine != null)
            {
                bioSync.StopCoroutine(bioSync.m_syncRoutine);
                bioSync.m_syncRoutine = null;
            }

            var state = bioSync.m_latestState;
            state.status = eBioscanStatus.Finished;
            state.progress = 1f;
            bioSync.m_latestState = state;
            bioSync.m_stateReplicator.State = state;
        }
    }
}