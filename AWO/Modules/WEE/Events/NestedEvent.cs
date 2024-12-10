using GameData;
using GTFO.API.Extensions;
using NestedType = AWO.Modules.WEE.WEE_NestedEvent.NestedMode;

namespace AWO.Modules.WEE.Events;

internal sealed class NestedEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.NestedEvent;

    protected override void TriggerCommon(WEE_EventData e)
    {
        int count = e.NestedEvent.EventsToActivate.Count;
        List<WardenObjectiveEventData> eventList;

        if (e.NestedEvent.Type == NestedType.RandomAny)
        {
            eventList = new();

            for (int i = 0; i < e.NestedEvent.MaxRandomEvents && i < e.NestedEvent.EventsToActivate.Count; i++)
            {
                int randIndex;
                do
                {
                    randIndex = SessionRand.Next(count);
                }
                while (!e.NestedEvent.AllowRepeatsInRandom && eventList.Contains(e.NestedEvent.EventsToActivate[randIndex]));

                eventList.Add(e.NestedEvent.EventsToActivate[randIndex]);
            }
        }
        else
        {
            eventList = e.NestedEvent.EventsToActivate;
        }

        WOManager.CheckAndExecuteEventsOnTrigger(eventList.ToIl2Cpp(), eWardenObjectiveEventTrigger.None);
    }
}