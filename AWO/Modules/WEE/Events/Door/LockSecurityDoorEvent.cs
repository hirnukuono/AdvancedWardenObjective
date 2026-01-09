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
        if (!TryGetZoneEntranceSecDoor(e, out var door)) return;

        var state = door.m_sync.GetCurrentSyncState();
        if (state.status == eDoorStatus.Open || state.status == eDoorStatus.Opening)
        {
            LogError("Door is open!");
            return;
        }
        else if (state.status == eDoorStatus.Closed_LockedWithKeyItem)
        {
            LogWarning("Door is Closed_LockedWithKeyItem, so there won't be any way to use the keycard if this door is unlocked again");
        }

        var sync = door.m_sync.TryCast<LG_Door_Sync>();
        if (sync == null) return;
        
        state.status = eDoorStatus.Closed_LockedWithNoKey;
        sync.m_stateReplicator.State = state;

        var intMessage = door.gameObject.GetComponentInChildren<Interact_MessageOnScreen>();
        if (intMessage != null && e.SpecialText != LocaleText.Empty)
        {
            intMessage.m_message = SerialLookupManager.ParseTextFragments(e.SpecialText);
        }
    }
}
