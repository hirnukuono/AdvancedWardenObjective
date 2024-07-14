using AWO.Modules.WEE;
using GameData;
using LevelGeneration;

namespace AWO.WEE.Events.SecDoor;

internal sealed class LockSecurityDoorEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.LockSecurityDoor;

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

        var eventData = new WardenObjectiveEventData
        {
            Type = eWardenObjectiveEventType.LockSecurityDoor,
            Layer = e.Layer,
            DimensionIndex = e.DimensionIndex,
            LocalIndex = e.LocalIndex
        };

        if (IsMaster)
            WorldEventManager.ExecuteEvent(eventData);

        var locks = door.gameObject.GetComponentInChildren<Interact_MessageOnScreen>();
        if (locks != null) 
            locks.m_message = e.SpecialText.ToString();
    }
}
