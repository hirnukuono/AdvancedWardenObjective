using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class ResetOnApproachDoorEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ResetOnApproachDoor;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) 
            return;

        var state = door.m_sync.GetCurrentSyncState();
        state.hasBeenApproached = false;
        var sync = door.m_sync.TryCast<LG_Door_Sync>();
        if (sync == null) return;
        sync.m_stateReplicator.State = state;

        var lastState = door.m_lastState;
        lastState.hasBeenApproached = false;
        door.m_lastState = lastState;
    }
}
