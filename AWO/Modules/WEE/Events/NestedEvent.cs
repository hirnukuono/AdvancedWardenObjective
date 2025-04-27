using BepInEx;
using BepInEx.Logging;
using GameData;
using NestedType = AWO.Modules.WEE.WEE_NestedEvent.NestedMode;

namespace AWO.Modules.WEE.Events;

internal sealed class NestedEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.NestedEvent;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (e.NestedEvent.Type != NestedType.ActivateAll)
        {
            EntryPoint.SessionRand.SyncStep(); // runs after TriggerCommon!
        }
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        var nested = e.NestedEvent;

        List<WardenObjectiveEventData> eventList = nested.Type switch
        {
            NestedType.RandomAny => SelectRandomUniform(nested),
            NestedType.RandomWeighted => SelectRandomWeighted(nested),
            _ => nested.EventsToActivate
        };
        
        ExecuteWardenEvents(eventList);
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
        int maxSpins = nested.MaxRandomEvents <= 0 ? nested.WheelOfEvents.Count : nested.MaxRandomEvents;
        List<WardenObjectiveEventData> eventList = new();
        List<WEE_NestedEvent.EventsOnRandomWeight> wheel = new(nested.WheelOfEvents);

        if (wheel.Count > 0)
        {
            do
            {
                Logger.Dev(LogLevel.Debug, $"WheelofEvents spin #{count + 1}");
                int randIndex = PickWeightedIndex(wheel);
                var rolledGroup = wheel[randIndex];
                eventList.AddRange(rolledGroup.Events);

                string debugName = $"{nested.WheelOfEvents.IndexOf(rolledGroup)}{(rolledGroup.DebugName.IsNullOrWhiteSpace() ? string.Empty : $" ({rolledGroup.DebugName})")}";
                Logger.Dev(LogLevel.Debug, $"Selected group index {debugName}");

                if (!nested.AllowRepeatsInRandom || (!rolledGroup.IsInfinite && --rolledGroup.RepeatCount <= 0))
                {
                    wheel.RemoveAt(randIndex);
                    Logger.Dev(LogLevel.Debug, $"Removed group index {debugName}. New wheel size is {wheel.Count}");
                }
                else
                {
                    wheel[randIndex] = rolledGroup;
                }
            } while (++count < maxSpins && wheel.Count > 0);
            Logger.Dev(LogLevel.Debug, "WheelofEvents is now done");
        }        

        eventList.InsertRange(0, nested.EventsToActivate);
        return eventList;
    }

    private static int PickWeightedIndex(List<WEE_NestedEvent.EventsOnRandomWeight> wheel)
    {
        float sum = wheel.Sum(part => part.Weight);
        float rand = EntryPoint.SessionRand.NextFloat() * sum;
        float cumulative = 0.0f;
        Logger.Dev(LogLevel.Debug, $"Rolled {rand} / {sum}");

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
