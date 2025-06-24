using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class TriggerSecurityDoorAlarmEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.TriggerSecurityDoorAlarm;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) return;

        string doorDebug = door.Gate.m_linksTo.m_zone.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_No_Formatting);

        if (door.m_locks.ChainedPuzzleToSolve != null)
        {
            door.m_sync.AttemptDoorInteraction(eDoorInteractionType.ActivateChainedPuzzle);
            LogDebug($"{doorDebug} alarm triggered!");
        }
        else
        {
            LogDebug($"{doorDebug} does not have any ChainedPuzzles to activate");
        }
    }
}
