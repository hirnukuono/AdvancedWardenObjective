using AWO.WEE.Events;

namespace AWO.Modules.WEE.Events.HUD;

internal sealed class AdjustAWOTimerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.AdjustAWOTimer;

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.TimerMods.TimeModifier = GetDuration(e);

        if (e.AdjustTimer.Speed != 0.0f)
            EntryPoint.TimerMods.SpeedModifier = e.AdjustTimer.Speed;

        if (e.AdjustTimer.UpdateText)
        {
            EntryPoint.TimerMods.CountupText = e.AdjustTimer.CustomText;
            EntryPoint.TimerMods.TimerColor = e.AdjustTimer.TimerColor;
        }
    }

    private static float GetDuration(WEE_EventData e)
    {
        if (e.AdjustTimer.Duration != 0.0f)
            return e.AdjustTimer.Duration;
        else if (e.Duration != 0.0f)
            return e.Duration;

        return e.AdjustTimer.Duration;
    }
}