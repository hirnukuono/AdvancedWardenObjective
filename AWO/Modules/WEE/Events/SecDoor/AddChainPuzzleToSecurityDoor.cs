using LevelGeneration;
using GameData;

namespace AWO.Modules.WEE.Events;

internal sealed class AddChainPuzzleToSecurityDoor : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.AddChainPuzzleToSecurityDoor;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) return;

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

        uint chainPuzzle = ResolveFieldsFallback((uint)e.SpecialNumber, e.ChainPuzzle);
        var block = ChainedPuzzleDataBlock.GetBlock(chainPuzzle);
        if (block == null || !block.internalEnabled)
        {
            LogError("Failed to find enabled ChainedPuzzleDataBlock!");
            return;
        }

        door.SetupChainedPuzzleLock(chainPuzzle);
        state.status = eDoorStatus.Closed_LockedWithChainedPuzzle;
        sync.m_stateReplicator.State = state;
        LogInfo($"Door into zone {door.Gate.m_linksTo.m_zone.m_navInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number)} was added ChainedPuzzle {chainPuzzle}");

        var locks = door.m_locks.TryCast<LG_SecurityDoor_Locks>();
        if (locks != null)
        {
            locks.m_intOpenDoor.InteractionMessage = "<color=red>[Warning: " + block.PublicAlarmName + " detected]</color>";
        }
    }
}
