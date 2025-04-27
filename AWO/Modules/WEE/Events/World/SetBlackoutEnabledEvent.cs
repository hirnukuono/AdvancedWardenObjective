namespace AWO.Modules.WEE.Events;

internal sealed class SetBlackoutEnabledEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetBlackoutEnabled;

    protected override void TriggerMaster(WEE_EventData e)
    {
        EntryPoint.BlackoutState.SetEnabled(e.Enabled);
    }
}
