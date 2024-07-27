using AWO.WEE.Events;
using GameData;
using LevelGeneration;
using Player;

namespace AWO.Modules.WEE.Events.Objective;

internal sealed class StartPortalEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.StartPortalMachine;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone))
        {
            LogError("Cannot find zone!");
            return;
        }

        LG_DimensionPortal portalMachine;
        try
        {
            portalMachine = EntryPoint.Portals[new GlobalZoneIndex(zone.DimensionIndex, zone.Layer.m_type, zone.LocalIndex)];
        }
        catch
        {
            LogError("Cannot find Portal!");
            return;
        }

        portalMachine.m_targetDimension = e.Portal.TargetDimension;
        portalMachine.m_targetZone = e.Portal.TargetZone;
        portalMachine.m_portalEventData = new WardenObjectiveEventData
        {
            Type = eWardenObjectiveEventType.DimensionWarpTeam,
            DimensionIndex = portalMachine.m_targetDimension,
            LocalIndex = portalMachine.m_targetZone,
            Delay = portalMachine.m_teleportDelay,
            ChainPuzzle = portalMachine.PortalChainPuzzle,
            UseStaticBioscanPoints = false
        };

        if (e.Enabled)
        {
            LogDebug("Activating portal...");
            pDimensionPortalState state = portalMachine.m_stateReplicator.State;
            state.isSequenceIncomplete = false;
            state.status = eDimensionPortalStatus.Inserting;
            portalMachine.m_stateReplicator.State = state;
            pItemData_Custom data2 = default;
            portalMachine.SetPortalKeyInserted(ref data2);
        }
    }
}
