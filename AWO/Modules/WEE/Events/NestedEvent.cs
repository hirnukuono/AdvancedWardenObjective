using AWO.WEE.Events;
using SNetwork;

namespace AWO.Modules.WEE.Events;

internal sealed class NestedEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.NestedEvent;

    protected override void TriggerMaster(WEE_EventData e)
    {
        foreach (var eventData in e.NestedEvent.EventsToActivate)
            if (SNet.IsMaster) 
                WorldEventManager.ExecuteEvent(eventData);
    }         
}