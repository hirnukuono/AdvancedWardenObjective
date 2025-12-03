using AWO.Modules.WEE.Replicators;
using ChainedPuzzles;
using GTFO.API;
using LevelGeneration;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class MoveExtractionWorldPositionEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.MoveExtractionWorldPosition;
    private static ScanPositionReplicator? EntranceScanReplicator;
    private static ScanPositionReplicator? ExitScanReplicator;
    
    public static bool HasFailed { get; private set; } = false;
    private string FailWarning(string scan, string tile) => $"[{Name}] An issue occured setting up the {scan} scan replicator. There might be an issue with the {tile} tile? This event will not work in this level!";

    protected override void OnSetup()
    {
        LevelAPI.OnFactoryDone += PostFactoryDone;
        LevelAPI.OnLevelCleanup += () => HasFailed = false;
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
        try
        {
            if (landing.m_puzzle.NRofPuzzles() != 1) return;
            var puzzleCore = landing.m_puzzle.GetPuzzle(0);
            var scanCore = puzzleCore.TryCast<CP_Bioscan_Core>();
            if (scanCore == null) return;

            var positionUpdater = landing.gameObject.AddComponent<ScanPositionReplicator>();
            positionUpdater.Setup(10u, scanCore, landing.m_marker, true);
            EntranceScanReplicator = positionUpdater;
        }
        catch
        {
            Logger.Warn(FailWarning("entrance", "elevator"));
            EntranceScanReplicator = null;
            HasFailed = true;
        }
    }

    private void TrackWinConditionScan(LG_LevelExitGeo exitGeo)
    {
        try
        {
            if (exitGeo.m_puzzle.NRofPuzzles() != 1) return;
            var puzzleCore = exitGeo.m_puzzle.GetPuzzle(0);
            var scanCore = puzzleCore.TryCast<CP_Bioscan_Core>();
            if (scanCore == null) return;

            var positionUpdater = exitGeo.gameObject.AddComponent<ScanPositionReplicator>();
            positionUpdater.Setup(20u, scanCore, exitGeo.m_marker, true);
            EntranceScanReplicator = positionUpdater;
        }
        catch
        {
            Logger.Warn(FailWarning("extraction", "exit"));
            ExitScanReplicator = null;
            HasFailed = true;
        }
    }

    protected override void TriggerMaster(WEE_EventData e)
    {
        Vector3 pos = GetPositionFallback(e.Position, e.SpecialText);
        EntranceScanReplicator?.TryUpdatePosition(pos);
        ExitScanReplicator?.TryUpdatePosition(pos);

        if (HasFailed)
        {
            LogError("Failed replicator setup during LG_Factory build");
        }
    }
}