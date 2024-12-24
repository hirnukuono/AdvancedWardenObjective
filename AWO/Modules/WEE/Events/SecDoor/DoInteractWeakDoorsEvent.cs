using GTFO.API;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class DoInteractWeakDoorsEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.DoInteractWeakDoorsInZone;
    public readonly static Dictionary<int, HashSet<LG_WeakDoor>> WeakDoors = new();

    protected override void OnSetup()
    {
        LevelAPI.OnLevelCleanup += OnLevelCleanup;
    }

    private void OnLevelCleanup()
    {
        WeakDoors.Clear();
    }

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (TryGetZone(e, out var zone) && WeakDoors.TryGetValue(zone.ID, out var weakDoors))
        {
            foreach (var weakDoor in weakDoors)
            {
                weakDoor.m_sync.AttemptDoorInteraction(e.Enabled ? eDoorInteractionType.Open : eDoorInteractionType.Close);
            }
        }
        else
        {
            LogError($"{e.LocalIndex} may have no WeakDoors?");
        }
    }
}
