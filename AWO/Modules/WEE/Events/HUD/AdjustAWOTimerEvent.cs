namespace AWO.Modules.WEE.Events;

internal sealed class AdjustAWOTimerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.AdjustAWOTimer;

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.TimerMods.TimeModifier = ResolveFieldFallback(e.Duration, e.AdjustTimer.Duration);

        if (e.AdjustTimer.Speed != 0.0f)
        {
            EntryPoint.TimerMods.SpeedModifier = e.AdjustTimer.Speed;
        }

        if (e.AdjustTimer.UpdateText)
        {
            EntryPoint.TimerMods.CountupText = e.AdjustTimer.CustomText;
            EntryPoint.TimerMods.TimerColor = e.AdjustTimer.TimerColor;
        }
    }
}