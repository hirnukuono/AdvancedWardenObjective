using AK;
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
        if (!TryGetCPInstance(e.ChainPuzzle, out var puzzleInstance) || !puzzleInstance.IsActive || puzzleInstance.IsSolved)
        {
            LogError($"An active ChainedPuzzle with index {e.ChainPuzzle} was not found!");
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
        for (int i = 0; i < puzzleInstance.NRofPuzzles(); i++)
        {
            if (i == count && count > 0) yield break;

            puzzleInstance.OnPuzzleDone(i);

            var clusterCore = puzzleInstance.m_chainedPuzzleCores[i].TryCast<CP_Cluster_Core>();
            if (clusterCore != null)
            {
                clusterCore.m_sync.SetStateData(eClusterStatus.Finished, 1.0f);
                continue;
            }

            var basicCore = puzzleInstance.m_chainedPuzzleCores[i].TryCast<CP_Bioscan_Core>();
            if (basicCore != null)
            {
                CoroutineManager.StartCoroutine(SolveBasicCore(basicCore).WrapToIl2Cpp());
            }

            yield return new WaitForSeconds(MasterRand.NextFloat() * 0.35f);
        }
    }

    static IEnumerator SolveBasicCore(CP_Bioscan_Core basicCore)
    {
        basicCore.m_playerScanner.TryCast<MonoBehaviour>()?.gameObject.SetActive(true);
        basicCore.m_spline?.SetVisible(false);

        basicCore.m_playerScanner.ResetScanProgression(1.0f);
        basicCore.m_sync.SetStateData(eBioscanStatus.Finished, 1.0f);

        yield return null;

        basicCore.m_sound.Post(EVENTS.BIOSCAN_PROGRESS_COUNTER_STOP);
        basicCore.m_spline?.TryCast<CP_Holopath_Spline>();
        
        yield return new WaitForSeconds(MasterRand.NextFloat() * 0.35f);

        CoroutineManager.BlinkOut(basicCore.gameObject);
    }
}