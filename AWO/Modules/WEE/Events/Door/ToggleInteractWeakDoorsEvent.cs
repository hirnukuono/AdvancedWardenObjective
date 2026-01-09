using LevelGeneration;
using static AWO.Modules.WEE.Events.DoInteractWeakDoorsEvent;

namespace AWO.Modules.WEE.Events;

internal sealed class ToggleInteractWeakDoorsEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ToggleInteractWeakDoorsInZone;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (TryGetZone(e, out var zone) && WeakDoors.TryGetValue(zone.ID, out var weakDoors))
        {
            foreach (var weakDoor in weakDoors)
            {
                weakDoor.m_inButtonOperation = !e.Enabled; // lazy

                foreach (var doorButton in weakDoor.m_buttons.Where(button => button != null))
                {
                    doorButton.m_anim.gameObject.SetActive(e.Enabled);
                    doorButton.m_enabled = e.Enabled;

                    if (e.Enabled)
                    {
                        var weakLock = doorButton.GetComponentInChildren<LG_WeakLock>();
                        if (weakLock != null && weakLock.Status != eWeakLockStatus.Unlocked)
                        {
                            doorButton.m_enabled = false;
                        }
                    }
                }
            }
        }
        else
        {
            LogError($"Zone may have no WeakDoors?");
        }
    }
}
