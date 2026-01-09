using AmorLib.Utils.Extensions;
using GTFO.API;
using LevelGeneration;
using System.Collections.Immutable;
using UnityEngine;

namespace AWO.Sessions;

public static class LG_Objects
{
    public static ImmutableDictionary<Type, HashSet<Component>> TrackedTypes { get; private set; } = ImmutableDictionary.CreateRange(new KeyValuePair<Type, HashSet<Component>>[]
    {
        new(typeof(LG_ComputerTerminal), new()),
        new(typeof(LG_DoorButton), new()),
        new(typeof(LG_HSUActivator_Core), new()),
        new(typeof(LG_LabDisplay), new()),
        new(typeof(LG_WeakLock), new())
    });

    static LG_Objects()
    {
        LevelAPI.OnLevelCleanup += Clear;
    }

    private static void Clear()
    {
        TrackedTypes.ForEachValue(set => set.Clear());
    }

    public static IEnumerable<T> TrackedList<T>() where T : Component
    {
        if (TrackedTypes.TryGetValue(typeof(T), out var set))
        {
            return set.Cast<T>();
        }
        return Enumerable.Empty<T>();
    }


    public static void AddToTrackedList(Component itemToAdd)
    {
        if (TrackedTypes.TryGetValue(itemToAdd.GetType(), out var set))
        {
            set.Add(itemToAdd);
        }
    }

    public static void RemoveFromTrackedList(Component itemToRemove)
    {
        if (TrackedTypes.TryGetValue(itemToRemove.GetType(), out var set))
        {
            set.Remove(itemToRemove);
        }
    }
}
