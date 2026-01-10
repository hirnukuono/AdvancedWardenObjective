using System.Text.Json.Serialization;

namespace AWO.Jsons;

[JsonConverter(typeof(ArrayableConverterFactory))]
public struct Arrayable<T>
{
    public IReadOnlyList<T> Values { get; }

    public readonly bool IsEmpty => Values.Count == 0;
    public readonly bool IsSingle => Values.Count == 1;    
    public readonly T First => IsEmpty ? default! : Values[0];

    public Arrayable()
    {
        Values = Array.Empty<T>();
    }

    public Arrayable(T value)
    {
        Values = new[] { value };
    }

    public Arrayable(IEnumerable<T> values)
    {
        Values = new List<T>(values);
    }

    public static implicit operator Arrayable<T>(T value) => new(value);
    public static implicit operator Arrayable<T>(T[] values) => new(values);
    public static implicit operator Arrayable<T>(List<T> values) => new(values);
    public static implicit operator T(Arrayable<T> arrayable) => arrayable.First;

    public override readonly string ToString()
    {
        return IsSingle ? Values[0]?.ToString() ?? "null" : $"[{string.Join(", ", Values)}]";
    }
}
