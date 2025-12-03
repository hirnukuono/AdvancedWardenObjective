using AmorLib.Utils.JsonElementConverters;
using static AWO.Modules.TSL.SerialLookupManager;

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
            EntryPoint.TimerMods.TimerTitleText = ParseLocaleText(e.AdjustTimer.TitleText);
        }

        if (e.AdjustTimer.UpdateText)
        {
            EntryPoint.TimerMods.TimerBodyText = ParseLocaleText(e.AdjustTimer.CustomText);
        }

        if ((e.AdjustTimer.UpdateText && e.AdjustTimer.CustomText != LocaleText.Empty) || e.AdjustTimer.UpdateColor)
        {
            EntryPoint.TimerMods.TimerColor = e.AdjustTimer.TimerColor;
        }
    }
}