using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class TriggerSecurityDoorAlarmEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.TriggerSecurityDoorAlarm;
    public override bool WhitelistArrayableGlobalIndex => true;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) return;

        var puzzleInstance = door.m_locks.ChainedPuzzleToSolve;
        if (puzzleInstance == null)
        {
            LogError($"Door does not have any chained puzzles to activate!");
        }
        else if (!puzzleInstance.IsSolved)
        {
            door.m_sync.AttemptDoorInteraction(eDoorInteractionType.ActivateChainedPuzzle);
        }
        else
        {
            LogWarning("Door's chained puzzles are already solved");
        }
    }
}
