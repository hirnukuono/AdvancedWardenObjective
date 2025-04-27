using AWO.Jsons;
using AWO.Modules.TSL;
using GameData;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class LockSecurityDoorEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.LockSecurityDoor;

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
            LogError("Door is open!");
            return;
        }

        state.status = eDoorStatus.Closed_LockedWithNoKey;
        sync.m_stateReplicator.State = state;

        WorldEventManager.ExecuteEvent(new()
        {
            Type = eWardenObjectiveEventType.LockSecurityDoor,
            Layer = e.Layer,
            DimensionIndex = e.DimensionIndex,
            LocalIndex = e.LocalIndex
        });

        var intMessage = door.gameObject.GetComponentInChildren<Interact_MessageOnScreen>();
        if (intMessage != null && e.SpecialText != LocaleText.Empty)
        {
            intMessage.m_message = SerialLookupManager.ParseTextFragments(e.SpecialText);
        }
    }
}
