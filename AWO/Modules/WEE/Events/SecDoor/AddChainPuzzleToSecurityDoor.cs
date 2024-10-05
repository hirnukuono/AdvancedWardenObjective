using AWO.Modules.WEE;
using LevelGeneration;

namespace AWO.WEE.Events.SecDoor;

internal sealed class AddChainPuzzleToSecurityDoor : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.AddChainPuzzleToSecurityDoor;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone))
        {
            LogError("Cannot find zone!");
            return;
        }

        if (!TryGetZoneEntranceSecDoor(zone, out var door))
        {
            LogError("Cannot find Security Door!");
            return;
        }

        var locks = door.m_locks.TryCast<LG_SecurityDoor_Locks>();
        var sync = door.m_sync.TryCast<LG_Door_Sync>();

        if (sync == null)
        {
            LogError("Door has no sync, wtf?");
            return;
        }

        var state = sync.GetCurrentSyncState();

        if (state.status == eDoorStatus.Open || state.status == eDoorStatus.Opening)
        {
            LogError("Door is already open!");
            return;
        }

        // sync.AttemptDoorInteraction(eDoorInteractionType.Close, 0f, 0f, door.gameObject.transform.position, null);
        door.SetupChainedPuzzleLock(e.ChainPuzzle);
        state.status = eDoorStatus.Closed_LockedWithChainedPuzzle;

        sync.m_stateReplicator.State = state;
        LogInfo($"Door into zone {zone.m_navInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number)} was added ChainedPuzzle {e.ChainPuzzle}");

        if (locks != null)
        {
            locks.m_intOpenDoor.InteractionMessage = "<color=red>[Warning: " + GameData.ChainedPuzzleDataBlock.GetBlock(e.ChainPuzzle).PublicAlarmName + " detected]</color>";
        }
    }
}
