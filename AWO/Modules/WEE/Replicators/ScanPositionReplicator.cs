using AIGraph;
using AWO.Networking;
using ChainedPuzzles;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using LevelGeneration;
using UnityEngine;

namespace AWO.Modules.WEE.Replicators;

internal struct ScanPositionState
{
    public Vector3 position;
    public int nodeID;
}

internal sealed class ScanPositionReplicator : MonoBehaviour, IStateReplicatorHolder<ScanPositionState>
{
    public Il2CppReferenceField<NavMarker> Marker;
    public Il2CppReferenceField<CP_Bioscan_Core> TrackingScan;
    public Il2CppValueField<bool> IsExitScan;

    public void Setup(uint id)
    {
        var scan = TrackingScan.Value;
        var defaultState = new ScanPositionState()
        {
            position = scan.transform.position,
            nodeID = scan.CourseNode.NodeID
        };

        if (!StateReplicator<ScanPositionState>.TryCreate(id, defaultState, LifeTimeType.Session, out var replicator, this))
        {
            Logger.Error("Failed to create ScanPositionReplicator!");
            return;
        }
        Replicator = replicator;
    }

    public void TryUpdatePosition(Vector3 position)
    {
        if (Replicator == null)
            return;

        if (Replicator.IsInvalid)
            return;

        if (AIG_CourseNode.TryGetCourseNode(position.GetDimension().DimensionIndex, position, 6.0f, out var node))
        {
            Replicator.SetState(new ScanPositionState()
            {
                position = position,
                nodeID = node.NodeID
            });
        }
    }

    [HideFromIl2Cpp]
    public StateReplicator<ScanPositionState> Replicator { get; private set; }

    [HideFromIl2Cpp]
    void IStateReplicatorHolder<ScanPositionState>.OnStateChange(ScanPositionState oldState, ScanPositionState state, bool isRecall)
    {
        var scan = TrackingScan.Value;
        scan.transform.position = state.position;
        if (scan.State.status != eBioscanStatus.Disabled)
        {
            //Refresh Scanner HUD Position
            scan.PlayerScanner.StopScan();
            scan.PlayerScanner.StartScan();
        }

        var marker = Marker.Value;
        marker.SetTrackingObject(scan.gameObject);

        if (AIG_CourseNode.GetCourseNode(state.nodeID, out var newNode))
        {
            scan.CourseNode.UnregisterBioscan(scan);
            scan.m_courseNode = newNode;
            newNode.RegisterBioscan(scan);
        }

        if (IsExitScan.Value)
        {
            var exitArea = scan.m_courseNode.m_zone;
            var navInfoText = exitArea.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Space);
            WOManager.SetObjectiveTextFragment(LG_LayerType.MainLayer, 0, eWardenTextFragment.EXTRACTION_ZONE, navInfoText);
            WOManager.SetObjectiveTextFragment(LG_LayerType.SecondaryLayer, 0, eWardenTextFragment.EXTRACTION_ZONE, navInfoText);
            WOManager.SetObjectiveTextFragment(LG_LayerType.ThirdLayer, 0, eWardenTextFragment.EXTRACTION_ZONE, navInfoText);
            WOManager.UpdateObjectiveGUIWithCurrentState(LG_LayerType.MainLayer, false);
            WOManager.UpdateObjectiveGUIWithCurrentState(LG_LayerType.SecondaryLayer, false);
            WOManager.UpdateObjectiveGUIWithCurrentState(LG_LayerType.ThirdLayer, false);
        }
    }
}
