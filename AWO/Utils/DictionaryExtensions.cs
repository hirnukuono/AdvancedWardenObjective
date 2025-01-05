using System.Collections.Concurrent;

namespace AWO.Utils;

public static class DictionaryExtensions
{
    public static TValue GetOrAddNew<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
        if (!dict.TryGetValue(key, out TValue? value))
        {
            value = new();
            dict[key] = value;
        }
        return value;
    }

    public static void ForEachValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, Action<TValue> action) where TKey : notnull
    {
        foreach (var value in dict.Values)
        {
            action(value);
        }
    }

    public static void ForEachValue<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, Action<TValue> action) where TKey : notnull
    {
        foreach (var value in dict.Values)
        {
            action(value);
        }
    }
}