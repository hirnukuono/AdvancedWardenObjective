using GTFO.API;
using LevelGeneration;
using UnityEngine;

namespace AWO.Sessions;

public static class LG_Objects
{
    public static Dictionary<Type, HashSet<Component>> TrackedTypes { get; private set; }

    static LG_Objects()
    {
        TrackedTypes = new()
        {
            { typeof(LG_ComputerTerminal), new HashSet<Component>() },
            { typeof(LG_LabDisplay), new HashSet<Component>() },
            { typeof(LG_DoorButton), new HashSet<Component>() },
            { typeof(LG_WeakLock), new HashSet<Component>() },
            { typeof(LG_HSUActivator_Core), new HashSet<Component>() }
        };

        LevelAPI.OnLevelCleanup += Clear;
    }

    private static void Clear()
    {
        TrackedTypes.Values.ToList().ForEach(list => list.Clear());
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
