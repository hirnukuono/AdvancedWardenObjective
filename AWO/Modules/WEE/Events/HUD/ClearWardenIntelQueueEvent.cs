namespace AWO.Modules.WEE.Events;

internal sealed class ClearWardenIntelQueueEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ClearWardenIntelQueue;

    protected override void TriggerCommon(WEE_EventData e)
    {
        GuiManager.PlayerLayer.m_wardenIntel.ResetSubObjectiveMesssagQueue();
    }
}