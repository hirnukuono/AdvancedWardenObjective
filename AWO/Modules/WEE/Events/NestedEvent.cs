using BepInEx;
using BepInEx.Logging;
using GameData;
using NestedType = AWO.Modules.WEE.WEE_NestedEvent.NestedMode;

namespace AWO.Modules.WEE.Events;

internal sealed class NestedEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.NestedEvent;

    protected override void TriggerCommon(WEE_EventData e)
    {
        var nested = e.NestedEvent;

        List<WardenObjectiveEventData> eventList = nested.Type switch
        {
            NestedType.RandomAny => SelectRandomUniform(nested),
            NestedType.RandomWeighted => SelectRandomWeighted(nested),
            _ => nested.EventsToActivate
        };
        
        ExecuteWardenEvents(eventList, nested.Type == NestedType.RandomWeighted);
    }

    private static List<WardenObjectiveEventData> SelectRandomUniform(WEE_NestedEvent nested)
    {
        List<WardenObjectiveEventData> eventList = new();

        int maxRolls = Math.Min(nested.MaxRandomEvents, nested.EventsToActivate.Count);
        for (int i = 0; i < maxRolls; i++)
        {
            int randIndex;
            do
            {
                randIndex = EntryPoint.SessionRand.NextInt(nested.EventsToActivate.Count);
            }
            while (!nested.AllowRepeatsInRandom && eventList.Contains(nested.EventsToActivate[randIndex]));

            eventList.Add(nested.EventsToActivate[randIndex]);
        }

        return eventList;
    }

    private static List<WardenObjectiveEventData> SelectRandomWeighted(WEE_NestedEvent nested)
    {
        int count = 0;
        int maxSpins = nested.MaxRandomEvents;
        List<WardenObjectiveEventData> eventList = nested.EventsToActivate.Where(e => e.Trigger == eWardenObjectiveEventTrigger.None || e.Trigger == eWardenObjectiveEventTrigger.OnStart).ToList();
        List<WardenObjectiveEventData> eventsOnMid = nested.EventsToActivate.Where(e => e.Trigger == eWardenObjectiveEventTrigger.OnMid).ToList();
        List<WEE_NestedEvent.EventsOnRandomWeight> wheel = new(nested.WheelOfEvents);

        if (wheel.Count > 0)
        {
            do
            {
                Logger.Verbose(LogLevel.Debug, $"WheelofEvents spin #{count + 1}");
                int randIndex = PickWeightedIndex(wheel);
                var rolledGroup = wheel[randIndex];
                eventList.AddRange(rolledGroup.Events);

                string debugName = $"{nested.WheelOfEvents.IndexOf(rolledGroup)} {(rolledGroup.DebugName.IsNullOrWhiteSpace() ? string.Empty : $"({rolledGroup.DebugName})")}";
                Logger.Verbose(LogLevel.Debug, $"Selected group index {debugName}");

                if (!nested.AllowRepeatsInRandom || (!rolledGroup.IsInfinite && --rolledGroup.RepeatCount <= 0))
                {
                    wheel.RemoveAt(randIndex);
                    Logger.Verbose(LogLevel.Debug, $"Removed group index {debugName}. New wheel size is {wheel.Count}");
                }
                else
                {
                    wheel[randIndex] = rolledGroup;
                }

                eventList.AddRange(eventsOnMid);
            } while (++count < maxSpins && wheel.Count > 0);
            Logger.Verbose(LogLevel.Debug, "WheelofEvents is now done");
        }

        eventList.AddRange(nested.EventsToActivate.Where(e => e.Trigger == eWardenObjectiveEventTrigger.OnEnd));
        return eventList;
    }

    private static int PickWeightedIndex(List<WEE_NestedEvent.EventsOnRandomWeight> wheel)
    {
        float sum = wheel.Sum(part => part.Weight);
        float rand = EntryPoint.SessionRand.NextFloat() * sum;
        float cumulative = 0.0f;
        Logger.Verbose(LogLevel.Debug, $"Rolled {rand} / {sum}");

        for (int i = 0; i < wheel.Count; i++)
        {
            cumulative += wheel[i].Weight;
            if (rand < cumulative)
            {
                return i;
            }
        }

        return wheel.Count - 1;
    }
}
