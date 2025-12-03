using System.Text.Json.Serialization;

namespace AWO.Jsons;

[JsonConverter(typeof(ArrayableConverterFactory))]
public readonly struct Arrayable<T>
{
    public IReadOnlyList<T> Values { get; }

    public bool IsSingle => Values.Count == 1;
    
    public Arrayable(T value) => Values = new[] { value };

    public Arrayable(IEnumerable<T> values) => Values = new List<T>(values);

    public static implicit operator Arrayable<T>(T value) => new(value);
    public static implicit operator Arrayable<T>(T[] values) => new(values);
    public static implicit operator Arrayable<T>(List<T> values) => new(values);

    public static readonly Arrayable<T> Empty = new(default(T)!);

    public override string ToString() => IsSingle ? Values[0]?.ToString() ?? "null" : $"[{string.Join(", ", Values)}]";
}

