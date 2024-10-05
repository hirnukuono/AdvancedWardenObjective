using AWO.Modules.WEE;
using LevelGeneration;

namespace AWO.WEE.Events.SecDoor;

internal sealed class TriggerSecurityDoorAlarmEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.TriggerSecurityDoorAlarm;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone))
        {
            LogError("Cannot find zone!");
            return;
        }

        if (!TryGetZoneEntranceSecDoor(zone, out var door))
        {
            LogError("Cannot find security door!");
            return;
        }

        if (door.m_locks.ChainedPuzzleToSolve != null)
        {
            door.m_sync.AttemptDoorInteraction(eDoorInteractionType.ActivateChainedPuzzle, 0.0f, 0.0f, default, null);
            LogDebug($"{zone.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_No_Formatting)} alarm triggered!");
        }
        else
        {
            LogDebug($"{zone.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_No_Formatting)} does not have any ChainedPuzzles to activate");
        }
    }
}
