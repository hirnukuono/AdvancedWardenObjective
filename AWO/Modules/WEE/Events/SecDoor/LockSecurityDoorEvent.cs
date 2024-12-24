using GameData;

namespace AWO.Modules.WEE.Events;

internal sealed class LockSecurityDoorEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.LockSecurityDoor;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) return;

        var eventData = new WardenObjectiveEventData
        {
            Type = eWardenObjectiveEventType.LockSecurityDoor,
            Layer = e.Layer,
            DimensionIndex = e.DimensionIndex,
            LocalIndex = e.LocalIndex
        };
        WorldEventManager.ExecuteEvent(eventData);

        var intMessage = door.gameObject.GetComponentInChildren<Interact_MessageOnScreen>();
        if (intMessage != null)
        {
            intMessage.m_message = e.SpecialText;
        }
    }
}
