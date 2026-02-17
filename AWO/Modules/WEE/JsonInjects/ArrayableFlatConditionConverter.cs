using GameData;
using Il2CppJsonNet;
using Il2CppJsonNet.Linq;
using InjectLib.JsonNETInjection.Converter;

namespace AWO.Modules.WEE.JsonInjects;

internal class ArrayableFlatConditionConverter : Il2CppJsonReferenceTypeConverter<WorldEventConditionPair>
{
    protected override WorldEventConditionPair Read(JToken jToken, WorldEventConditionPair existingValue, JsonSerializer serializer)
    {
        switch (jToken.Type)
        {
            case JTokenType.Array:
                var arr = jToken.Cast<JArray>();
                if (arr.Count > 0)
                    return ReadToken(arr[0]);
                return new();

            case JTokenType.Object:
                return ReadToken(jToken);

            default:
                return new();
        }
    }

    private static WorldEventConditionPair ReadToken(JToken token)
    {
        var jObject = token.Cast<JObject>();
        WorldEventConditionPair result = new();
        if (jObject.TryGetValue(nameof(WorldEventConditionPair.ConditionIndex), out var idx))
            result.ConditionIndex = (int) idx;
        if (jObject.TryGetValue(nameof(WorldEventConditionPair.IsTrue), out var isTrue))
            result.IsTrue = (bool) isTrue;
        return result;
    }

    protected override void Write(JsonWriter writer, WorldEventConditionPair value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(nameof(WorldEventConditionPair.ConditionIndex));
        writer.WriteValue(value.ConditionIndex);
        writer.WritePropertyName(nameof(WorldEventConditionPair.IsTrue));
        writer.WriteValue(value.IsTrue);
        writer.WriteEndObject();
    }
}