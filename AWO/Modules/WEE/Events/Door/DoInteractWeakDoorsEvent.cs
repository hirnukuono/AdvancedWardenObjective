using AWO.Modules.WEE.Events.Door.WeakDoors;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class DoInteractWeakDoorsEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.DoInteractWeakDoorsInZone;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (TryGetZone(e, out var zone) && WeakDoorInfo.WeakDoors.TryGetValue(zone.ID, out var weakDoors))
        {
            foreach (var weakDoor in weakDoors)
            {
                weakDoor.PlayButtonAnimation(e.Enabled);
            }
        }
        else
        {
            LogError($"Zone may have no WeakDoors?");
        }
    }

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (TryGetZone(e, out var zone) && WeakDoorInfo.WeakDoors.TryGetValue(zone.ID, out var weakDoors))
        {
            foreach (var weakDoor in weakDoors)
            {
                weakDoor.DoInteract(e.Enabled);
            }
        }
        else
        {
            LogError($"Zone may have no WeakDoors?");
        }
    }
}