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
            switch (clusterCore.m_sync.GetCurrentState().status)
            {
                case eClusterStatus.Finished:
                    return;
                case eClusterStatus.Disabled:
                    if (finished)
                        break;
                    else
                        return;
                case eClusterStatus.SplineReveal:
                    clusterCore.m_spline?.Cast<CP_Holopath_Spline>().m_sound.Post(EVENTS.BIOSCAN_TUBE_EMITTER_STOP);
                    break;
                case eClusterStatus.ClusterActive:
                    foreach (var child in clusterCore.m_childCores)
                        SolveCore(child);
                    break;
            }
            clusterCore.m_sync.SetStateData(finished ? eClusterStatus.Finished : eClusterStatus.Disabled, 1f);
        }
        else
        {
            var bioCore = core.Cast<CP_Bioscan_Core>();
            switch (bioCore.m_sync.GetCurrentState().status)
            {
                case eBioscanStatus.Finished:
                    return;
                case eBioscanStatus.SplineReveal:
                    bioCore.m_spline?.Cast<CP_Holopath_Spline>().m_sound.Post(EVENTS.BIOSCAN_TUBE_EMITTER_STOP);
                    break;
                case eBioscanStatus.Disabled:
                    if (finished)
                        break;
                    else
                        return;
                default:
                    break;
            }
            if (bioCore.IsMovable && !bioCore.m_movingComp.OnlyMoveWhenScannig)
                bioCore.m_movingComp.StopMoving();
            bioCore.m_sync.SetStateData(finished ? eBioscanStatus.Finished : eBioscanStatus.Disabled, 1f);
        }
    }
}