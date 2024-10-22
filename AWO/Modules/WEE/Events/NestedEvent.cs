using AWO.WEE.Events;
using GameData;

namespace AWO.Modules.WEE.Events;

internal sealed class NestedEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.NestedEvent;

    protected override void OnSetup()
    {
        LevelEvents.OnLevelBuildDoneLate += PostFactoryDone;
    }

    private void PostFactoryDone()
    {
        int seed = RundownManager.GetActiveExpeditionData().sessionSeed;
        EntryPoint.SessionSeed = new(seed);
        Logger.Info($"SessionSeed {seed}");
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        int length = e.NestedEvent.EventsToActivate.Length;
        List<WardenObjectiveEventData> eventList;

        if ((byte)e.NestedEvent.Type == 1)
        {
            eventList = new();

            for (int i = 0; i < e.NestedEvent.MaxRandomEvents; i++)
            {
                int randIndex;

                do
                {
                    randIndex = EntryPoint.SessionSeed.Next(length);
                }
                while (!e.NestedEvent.AllowRepeatsInRandom && eventList.Contains(e.NestedEvent.EventsToActivate[randIndex]));

                eventList.Add(e.NestedEvent.EventsToActivate[randIndex]);
            }
        }
        else
        {
            eventList = new(e.NestedEvent.EventsToActivate);
        }

        foreach (var eventData in eventList)
        {
            WorldEventManager.ExecuteEvent(eventData);
        }
    }
}