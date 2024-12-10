using GameData;
using GTFO.API;
using Player;

namespace AWO.Modules.WEE.Events;

internal sealed class StartPortalEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.StartPortalMachine;

    protected override void OnSetup()
    {
        LevelAPI.OnLevelCleanup += OnLevelCleanup;
    }

    private void OnLevelCleanup()
    {
        EntryPoint.Portals.Clear();
    }

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) return;

        if (!EntryPoint.Portals.TryGetValue(new(zone.DimensionIndex, zone.Layer.m_type, zone.LocalIndex), out var portalMachine))
        {
            LogError("Cannot find Portal!");
            return;
        }

        portalMachine.m_targetDimension = e.Portal.TargetDimension;
        portalMachine.m_targetZone = e.Portal.TargetZone;
        portalMachine.m_portalEventData = new()
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
