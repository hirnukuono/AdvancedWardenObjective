using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class PickupSentries : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.PickupSentries;

    protected override void TriggerMaster(WEE_EventData e)
    {
        foreach (var item in Dimension.WarpableObjects)
        {
            var sentry = item.TryCast<SentryGunInstance>();
            if (sentry != null && sentry.LocallyPlaced)
            {
                sentry.m_sync.WantItemAction(sentry.Owner, SyncedItemAction_New.PickUp);
            }
        }
    }
}