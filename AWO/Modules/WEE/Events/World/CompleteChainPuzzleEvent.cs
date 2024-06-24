using AK;
using AWO.Modules.WEE;
using GTFO.API.Utilities;
using ChainedPuzzles;
using SNetwork;
using System.Collections;
using UnityEngine;

namespace AWO.WEE.Events.World;

internal class CompleteChainPuzzleEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ForceCompleteChainPuzzle;

    protected override void TriggerMaster(WEE_EventData e)
    {
        var cp = GetCPInstance(ChainedPuzzleManager.Current.m_instances, e.ChainPuzzle);

        if (cp == null || !cp.IsActive || cp.IsSolved)
        {
            Logger.Error("AdvancedWardenObjective - Active ChainPuzzle not found");
            return;
        }
        
        for (int i = 0; i < cp.m_chainedPuzzleCores.Length; i++)
        {
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
        }

        cp.OnPuzzleDone(cp.m_chainedPuzzleCores.Length - 1);
        Logger.Debug("AdvancedWardenObjective - Probably ignore the null refs?");
        cp.m_chainedPuzzleCores = null;
    }

    private static ChainedPuzzleInstance? GetCPInstance(Il2CppSystem.Collections.Generic.List<ChainedPuzzleInstance> m_instances, uint ID)
    {
        foreach (var instance in m_instances)
            if (instance.Data.persistentID == ID)
                return instance;

        return null;
    }

    static IEnumerator SolveBasicCore(CP_Bioscan_Core basicCore)
    {
        if (basicCore == null)
            yield break;

        basicCore.m_playerScanner.TryCast<MonoBehaviour>().gameObject.active = true;
        basicCore.m_spline.SetVisible(false);
        if (IsMaster)
        {
            basicCore.m_playerScanner.ResetScanProgression(1.0f);
            basicCore.m_sync.SetStateData(eBioscanStatus.Finished, 1.0f);

            yield return null;

            var spline = basicCore.m_spline.TryCast<CP_Holopath_Spline>();
            basicCore.m_sound.Post(EVENTS.BIOSCAN_PROGRESS_COUNTER_STOP, isGlobal: true);
            spline.m_sound.Post(EVENTS.BIOSCAN_TUBE_EMITTER_STOP, isGlobal: true); 
        }
        yield return new WaitForSeconds(RNG.Float01 * 0.35f);

        CoroutineManager.BlinkOut(basicCore.gameObject);

        yield break;
    }
}