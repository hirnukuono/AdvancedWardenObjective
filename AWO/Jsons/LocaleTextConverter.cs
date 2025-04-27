using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWO.Jsons;

public class LocaleTextConverter : JsonConverter<LocaleText>
{
    public override bool HandleNull => true;

    public override LocaleText Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => new LocaleText(reader.GetString()!),
            JsonTokenType.Number => new LocaleText(reader.GetUInt32()),
            JsonTokenType.Null => LocaleText.Empty,
            _ => throw new JsonException($"LocaleTextJson type: {reader.TokenType} is not implemented!"),
        };
    }

    public override void Write(Utf8JsonWriter writer, LocaleText value, JsonSerializerOptions options)
    {
        if (value.ID != 0)
        {
            writer.WriteNumberValue(value.ID);
        }
        else
        {
            writer.WriteStringValue(value.RawText);
        }
    }
}
