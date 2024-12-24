namespace AWO.Utils;

internal static class DictionaryExtensions
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
}