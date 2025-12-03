using AIGraph;
using AmorLib.Networking.StateReplicators;
using AmorLib.Utils;
using ChainedPuzzles;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using LevelGeneration;
using UnityEngine;

namespace AWO.Modules.WEE.Replicators;

public struct ScanPositionState
{
    public Vector3 position;
    public int nodeID;
}

public sealed class ScanPositionReplicator : MonoBehaviour, IStateReplicatorHolder<ScanPositionState>
{
    [HideFromIl2Cpp]
    public StateReplicator<ScanPositionState>? Replicator { get; private set; }
    public CP_Bioscan_Core? TrackingScan;
    public NavMarker? Marker;
    public bool IsExitScan;

    public void Setup(uint id, CP_Bioscan_Core scan, NavMarker marker, bool isExit) // reserved ids: 10u, 20u
    {
        Replicator = StateReplicator<ScanPositionState>.Create(id, new() 
        {
            position = scan.transform.position,
            nodeID = scan.CourseNode.NodeID
        }, LifeTimeType.Session, this);
        
        TrackingScan = scan;
        Marker = marker;
        IsExitScan = isExit;
    }

    public void OnDestroy()
    {
        Replicator?.Unload();
    }

    public void TryUpdatePosition(Vector3 position)
    {
        var node = CourseNodeUtil.GetCourseNode(position, position.GetDimension().DimensionIndex);
        if (node != null)
        {
            Replicator?.SetState(new()
            {
                position = position,
                nodeID = node.NodeID
            });
        }
    }

    public void OnStateChange(ScanPositionState oldState, ScanPositionState state, bool isRecall)
    {
        if (TrackingScan == null)
        {
            Logger.Error("ScanPositionReplicator", "TrackingScan is null!");
            return;
        }

        TrackingScan.transform.position = state.position;
        if (TrackingScan.State.status != eBioscanStatus.Disabled) // Refresh Scanner HUD Position
        {            
            TrackingScan.PlayerScanner.StopScan();
            TrackingScan.PlayerScanner.StartScan();
        }

        Marker?.SetTrackingObject(TrackingScan.gameObject);

        if (AIG_CourseNode.GetCourseNode(state.nodeID, out var newNode)) // Register scan in new node
        {
            TrackingScan.CourseNode.UnregisterBioscan(TrackingScan);
            TrackingScan.m_courseNode = newNode;
            newNode.RegisterBioscan(TrackingScan);
        }

        if (IsExitScan) // Update [EXTRACTION_ZONE] text 
        {
            var extractZone = TrackingScan.m_courseNode.m_zone;
            string navInfoText = extractZone.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Space);
            foreach (var layer in Enum.GetValues<LG_LayerType>())
            {
                if (WOManager.HasWardenObjectiveDataForLayer(layer))
                {
                    WOManager.SetObjectiveTextFragment(layer, WOManager.GetCurrentChainIndex(layer), eWardenTextFragment.EXTRACTION_ZONE, navInfoText);
                    WOManager.UpdateObjectiveGUIWithCurrentState(layer, false, false, true);
                }
            }
        }
    }
}
