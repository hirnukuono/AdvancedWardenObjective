using GTFO.API.Utilities;
using SNetwork;
using AWO.WEE.Events;
using UnityEngine;

namespace AWO.Modules.WEE.Events.HUD;

internal sealed class AdjustAWOTimerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.AdjustAWOTimer;

    protected override void TriggerCommon(WEE_EventData e)
    {
        EntryPoint.TimerModifier = e.Duration;
        if (e.Enabled) EntryPoint.SpeedModifier = e.Speed;
    }
}