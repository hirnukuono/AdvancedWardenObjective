using AWO.Modules.WEE.Replicators;
using BepInEx;
using ChainedPuzzles;
using GTFO.API;
using LevelGeneration;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class MoveExtractionWorldPositionEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.MoveExtractionWorldPosition;
    private ScanPositionReplicator? EntranceScanReplicator;
    private ScanPositionReplicator? ExitScanReplicator;

    protected override void OnSetup()
    {
        LevelAPI.OnBuildDone += PostFactoryDone;
    }

    private void PostFactoryDone()
    {
        var landing = WOManager.m_elevatorExitWinConditionItem?.TryCast<ElevatorShaftLanding>();
        var exitgeo = WOManager.m_customGeoExitWinConditionItem?.TryCast<LG_LevelExitGeo>();

        if (landing != null)
        {
            TrackWinConditionScan(landing);
        }

        if (exitgeo != null)
        {
            TrackWinConditionScan(exitgeo);
        }
    }

    private void TrackWinConditionScan(ElevatorShaftLanding landing)
    {
        if (landing.m_puzzle.NRofPuzzles() > 1) return;

        var puzzleCore = landing.m_puzzle.GetPuzzle(0);
        var scanCore = puzzleCore.TryCast<CP_Bioscan_Core>();
        if (scanCore == null) return;

        var positionUpdater = landing.gameObject.AddComponent<ScanPositionReplicator>();
        positionUpdater.Marker.Set(landing.m_marker);
        positionUpdater.TrackingScan.Set(scanCore);
        positionUpdater.IsExitScan.Set(true);
        positionUpdater.Setup(10u);
        EntranceScanReplicator = positionUpdater;
    }

    private void TrackWinConditionScan(LG_LevelExitGeo exitgeo)
    {
        if (exitgeo.m_puzzle.NRofPuzzles() > 1) return;

        var puzzleCore = exitgeo.m_puzzle.GetPuzzle(0);
        var scanCore = puzzleCore.TryCast<CP_Bioscan_Core>();
        if (scanCore == null) return;

        var positionUpdater = exitgeo.gameObject.AddComponent<ScanPositionReplicator>();
        positionUpdater.Marker.Set(exitgeo.m_marker);
        positionUpdater.TrackingScan.Set(scanCore);
        positionUpdater.IsExitScan.Set(true);
        positionUpdater.Setup(20u);
        ExitScanReplicator = positionUpdater;
    }

    protected override void TriggerMaster(WEE_EventData e)
    {
        LogDebug("Position updated");

        Vector3 pos = GetPositionFallback(e.Position, e.SpecialText);
        EntranceScanReplicator?.TryUpdatePosition(pos);
        ExitScanReplicator?.TryUpdatePosition(pos);
    }
}
