using AWO.Modules.TSL;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class SetDoorInteractionEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetDoorInteraction;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) 
            return;

        var locks = door.m_locks.TryCast<LG_SecurityDoor_Locks>();
        if (locks == null) 
            return;

        var state = door.m_sync.GetCurrentSyncState();
        switch (state.status)
        {
            case eDoorStatus.Open:
            case eDoorStatus.Opening:
                LogError("Door is already open!");
                break;

            case eDoorStatus.Closed_LockedWithBulkheadDC:
            case eDoorStatus.Closed_LockedWithPowerGenerator:
            case eDoorStatus.Closed_LockedWithNoKey:
                locks.m_intCustomMessage.m_message = SerialLookupManager.ParseTextFragments(e.SpecialText);
                break;

            case eDoorStatus.Closed_LockedWithKeyItem:
                locks.m_intUseKeyItem.m_msgNeedItemHeader = SerialLookupManager.ParseTextFragments(e.SpecialText);
                break;

            case eDoorStatus.Closed:
            case eDoorStatus.Closed_LockedWithChainedPuzzle:
            case eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm:
            case eDoorStatus.Unlocked:
                locks.m_intOpenDoor.InteractionMessage = SerialLookupManager.ParseTextFragments(e.SpecialText);
                break;

            default:
                LogError("Door is in an unsupported state!");
                break;
        }
    }
}
