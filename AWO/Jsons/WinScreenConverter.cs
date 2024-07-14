using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWO.Jsons;

public class WinScreenConverter : JsonConverter<WinScreen>
{
    public override bool HandleNull => true;

    public override WinScreen Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => new WinScreen(reader.GetString()),
            JsonTokenType.Number => new WinScreen(reader.GetInt32()),
            JsonTokenType.Null => WinScreen.Empty,
            _ => throw new JsonException($"WinScreenJson type: {reader.TokenType} is not implemented!"),
        };
    }

    public override void Write(Utf8JsonWriter writer, WinScreen value, JsonSerializerOptions options)
    {
        if (value.PagePath != string.Empty)
        {
            writer.WriteStringValue(value.PagePath);
        }
    }
}
