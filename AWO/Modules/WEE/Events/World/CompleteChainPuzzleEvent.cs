using AK;
using AWO.Modules.WEE;
using ChainedPuzzles;
using System.Collections;
using UnityEngine;
using Il2CppCPInstanceList = Il2CppSystem.Collections.Generic.List<ChainedPuzzles.ChainedPuzzleInstance>;

namespace AWO.WEE.Events.World;

internal class CompleteChainPuzzleEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ForceCompleteChainPuzzle;

    protected override void TriggerMaster(WEE_EventData e)
    {
        var cp = GetCPInstance(ChainedPuzzleManager.Current.m_instances, e.ChainPuzzle);

        if (cp == null || !cp.IsActive || cp.IsSolved)
        {
            LogError("An active ChainedPuzzle was not found");
            return;
        }
        
        CoroutineManager.StartCoroutine(SolvePuzzleCores(cp, e.Count).WrapToIl2Cpp());
    }

    private static ChainedPuzzleInstance? GetCPInstance(Il2CppCPInstanceList m_instances, uint ID)
    {
        foreach (var instance in m_instances)
            if (instance.Data.persistentID == ID)
                return instance;

        return null;
    }

    static IEnumerator SolvePuzzleCores(ChainedPuzzleInstance cp, int count)
    {
        for (int i = 0; i < cp.NRofPuzzles(); i++)
        {
            if (i == count && count > 0)
                yield break;

            cp.OnPuzzleDone(i);

            var clusterCore = cp.m_chainedPuzzleCores[i].TryCast<CP_Cluster_Core>();
            if (clusterCore != null)
            {
                if (IsMaster)
                {
                    clusterCore.m_sync.SetStateData(eClusterStatus.Finished, 1.0f);
                }
                continue;
            }

            var basicCore = cp.m_chainedPuzzleCores[i].TryCast<CP_Bioscan_Core>();
            if (basicCore != null)
                CoroutineManager.StartCoroutine(SolveBasicCore(basicCore).WrapToIl2Cpp());

            yield return new WaitForSeconds(RNG.Float01 * 0.35f);
        }
    }

    static IEnumerator SolveBasicCore(CP_Bioscan_Core basicCore)
    {
        if (basicCore == null)
            yield break;

        basicCore.m_playerScanner.TryCast<MonoBehaviour>()?.gameObject.SetActive(true);
        basicCore.m_spline.SetVisible(false);

        if (IsMaster)
        {
            basicCore.m_playerScanner.ResetScanProgression(1.0f);
            basicCore.m_sync.SetStateData(eBioscanStatus.Finished, 1.0f);

            yield return null;

            basicCore.m_sound.Post(EVENTS.BIOSCAN_PROGRESS_COUNTER_STOP, isGlobal: true);
            try
            {
                var spline = basicCore.m_spline.TryCast<CP_Holopath_Spline>();
                spline?.m_sound.Post(EVENTS.BIOSCAN_TUBE_EMITTER_STOP, isGlobal: true);
            }
            catch
            {
                Logger.Warn("[ForceCompleteChainPuzzleEvent] A CP_Bioscan_Core has no spline, skipping killing sound");
            }
        }

        yield return new WaitForSeconds(RNG.Float01 * 0.35f);

        CoroutineManager.BlinkOut(basicCore.gameObject);
    }
}