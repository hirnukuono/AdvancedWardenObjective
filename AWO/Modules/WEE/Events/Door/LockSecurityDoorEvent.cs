using AmorLib.Utils.JsonElementConverters;
using AWO.Modules.TSL;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class LockSecurityDoorEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.LockSecurityDoor;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) 
            return;

        var state = door.m_sync.GetCurrentSyncState();
        if (e.SpecialText != LocaleText.Empty)
        {
            var locks = door.m_locks.Cast<LG_SecurityDoor_Locks>();
            locks.m_lockedWithNoKeyInteractionText = new LocaleText(SerialLookupManager.ParseTextFragments(e.SpecialText));
            // JFS in case the door status was somehow received first
            if (state.status == eDoorStatus.Closed_LockedWithNoKey)
                locks.m_intCustomMessage.m_message = locks.m_lockedWithNoKeyInteractionText;
        }

        if (IsMaster)
            LockSecDoor(door, state);
    }

    private void LockSecDoor(LG_SecurityDoor door, pDoorState state)
    {
        if (state.status == eDoorStatus.Open || state.status == eDoorStatus.Opening)
        {
            LogError("Door is open!");
            return;
        }
        if (state.status == eDoorStatus.Closed_LockedWithKeyItem)
        {
            LogWarning($"Door is {state.status}, so there won't be any way to use the keycard if this door is unlocked again");
        }

        var sync = door.m_sync.TryCast<LG_Door_Sync>();
        if (sync == null)
            return;

        state.status = eDoorStatus.Closed_LockedWithNoKey;
        sync.m_stateReplicator.State = state;
    }
}
