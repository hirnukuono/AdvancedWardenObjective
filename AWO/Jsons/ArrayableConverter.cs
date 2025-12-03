using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWO.Jsons;

public class ArrayableConverter<T> : JsonConverter<Arrayable<T>>
{
    public override Arrayable<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = JsonSerializer.Deserialize<List<T>>(ref reader, options);
            return new Arrayable<T>(list!);
        }
        else
        {
            var value = JsonSerializer.Deserialize<T>(ref reader, options);
            return new Arrayable<T>(value!);
        }
    }

    public override void Write(Utf8JsonWriter writer, Arrayable<T> value, JsonSerializerOptions options)
        => throw new NotSupportedException("Serialization not supported yet.");
}

