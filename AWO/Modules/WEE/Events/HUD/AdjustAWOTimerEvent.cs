using BepInEx;
using static AWO.Modules.TerminalSerialLookup.SerialLookupManager;

namespace AWO.Modules.WEE.Events;

internal sealed class AdjustAWOTimerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.AdjustAWOTimer;

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.TimerMods.TimeModifier = ResolveFieldsFallback(e.Duration, e.AdjustTimer.Duration, false);

        if (e.AdjustTimer.Speed != 0.0f)
        {
            EntryPoint.TimerMods.SpeedModifier = e.AdjustTimer.Speed;
        }

        if (e.AdjustTimer.UpdateTitleText)
        {
            EntryPoint.TimerMods.TimerTitleText = new(ParseTextFragments(e.AdjustTimer.TitleText));
        }

        if (e.AdjustTimer.UpdateText)
        {
            EntryPoint.TimerMods.TimerBodyText = new(ParseTextFragments(e.AdjustTimer.CustomText));
        }

        if ((e.AdjustTimer.UpdateText && !e.AdjustTimer.CustomText.ToString().IsNullOrWhiteSpace()) || e.AdjustTimer.UpdateColor)
        {
            EntryPoint.TimerMods.TimerColor = e.AdjustTimer.TimerColor;
        }
    }
}