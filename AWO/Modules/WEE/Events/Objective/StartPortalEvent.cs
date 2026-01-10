using AmorLib.Utils;
using GameData;
using GTFO.API;
using LevelGeneration;
using Player;

namespace AWO.Modules.WEE.Events;

internal sealed class StartPortalEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.StartPortalMachine;

    public static Dictionary<GlobalZoneIndex, LG_DimensionPortal> Portals { get; set; } = new();

    protected override void OnSetup()
    {
        LevelAPI.OnLevelCleanup += OnLevelCleanup;
    }

    private void OnLevelCleanup()
    {
        Portals.Clear();
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) 
            return;

        if (!Portals.TryGetValue(zone.ToStruct(), out var portalMachine))
        {
            LogError("Cannot find Portal!");
            return;
        }

        e.Portal ??= new();
        portalMachine.m_targetDimension = e.Portal.TargetDimension;
        portalMachine.m_teleportDelay = e.Portal.TeleportDelay;
        portalMachine.m_portalEventData = new()
        {
            Type = e.Portal.PreventPortalWarpTeamEvent ? eWardenObjectiveEventType.None : eWardenObjectiveEventType.DimensionWarpTeam,
            DimensionIndex = portalMachine.m_targetDimension,
            Delay = portalMachine.m_teleportDelay
        };

        if (IsMaster && e.Enabled)
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
