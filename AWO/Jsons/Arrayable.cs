using System.Text.Json.Serialization;

namespace AWO.Jsons;

[JsonConverter(typeof(ArrayableConverterFactory))]
public struct Arrayable<T> : IEquatable<Arrayable<T>>
{
    public IReadOnlyList<T> Values { get; }

    public readonly T First => Values?.Count > 0 ? Values[0] : default!;

    public readonly bool IsSingle => Values.Count == 1;

    public Arrayable(T value)
    {
        Values = new[] { value };
    }

    public Arrayable(IEnumerable<T> values)
    {
        Values = new List<T>(values);
    }

    public static implicit operator Arrayable<T>(T value) => new(value);
    public static implicit operator T(Arrayable<T> arrayable)
    {
        if (arrayable.Values?.Count > 0)
            return arrayable.Values[0];

        return default!;
    }

    //public static readonly Arrayable<T> Empty = new(default(T)!);

    public readonly bool Equals(Arrayable<T> other)
    {
        if (ReferenceEquals(Values, other.Values))
            return true;

        if (Values == null || other.Values == null)
            return Values == null && other.Values == null;

        if (Values.Count != other.Values.Count)
            return false;

        var comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < Values.Count; i++)
        {
            if (!comparer.Equals(Values[i], other.Values[i]))
            {
                return false;
            }
        }

       return true;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Arrayable<T> other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        if (Values == null)
            return 0;

        int hash = 17;
        var comparer = EqualityComparer<T>.Default;
        foreach (var item in Values)
        {
            hash = hash * 31 + (item != null ? comparer.GetHashCode(item) : 0);
        }

        return hash;
    }

    public static bool operator ==(Arrayable<T> left, Arrayable<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Arrayable<T> left, Arrayable<T> right)
    {
        return !(left == right);
    }
}
