using AWO.Modules.WEE.Events.Door.WeakDoors;

namespace AWO.Modules.WEE.Events;

internal sealed class ToggleInteractWeakDoorsEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ToggleInteractWeakDoorsInZone;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (TryGetZone(e, out var zone) && WeakDoorInfo.WeakDoors.TryGetValue(zone.ID, out var weakDoors))
        {
            foreach (var weakDoor in weakDoors)
            {
                weakDoor.SetCanInteract(e.Enabled);
            }
        }
        else
        {
            LogError($"Zone may have no WeakDoors?");
        }
    }
}
