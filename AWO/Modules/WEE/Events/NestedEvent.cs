﻿using AWO.WEE.Events;

namespace AWO.Modules.WEE.Events;

internal sealed class NestedEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.NestedEvent;

    protected override void TriggerCommon(WEE_EventData e)
    {
        foreach (var eventData in e.NestedEvent.EventsToActivate)
            WorldEventManager.ExecuteEvent(eventData);
    }
}