using GameData;
using Il2CppJsonNet;
using Il2CppJsonNet.Linq;
using InjectLib.JsonNETInjection.Converter;
using LevelGeneration;

namespace AWO.Modules.WEE.JsonInjects;

internal class DimensionIndexConverter : Il2CppJsonUnmanagedTypeConverter<eDimensionIndex>
{
    protected override eDimensionIndex Read(JToken jToken, eDimensionIndex existingValue, JsonSerializer serializer)
    {
        switch (jToken.Type)
        {
            case JTokenType.Array:
                if (jToken is JArray arr && arr?.Count > 0)
                    return ParseEnum(arr[0]);
                return eDimensionIndex.Reality;

            case JTokenType.Integer:
            case JTokenType.String:
                return ParseEnum(jToken);

            default:
                return eDimensionIndex.Reality;
        }
    }

    protected override void Write(JsonWriter writer, eDimensionIndex value, JsonSerializer serializer)
    {
        writer.WriteValue((int)value);
    }

    protected override Il2CppSystem.Object ToIl2CppObject(eDimensionIndex value)
    {
        return new Il2CppSystem.Int32() { m_value = (int)value }.BoxIl2CppObject();
    }

    private static eDimensionIndex ParseEnum(JToken jToken)
    {
        if (jToken.Type == JTokenType.Integer)
            return (eDimensionIndex)(int)jToken;

        if (jToken.Type == JTokenType.String && Enum.TryParse<eDimensionIndex>((string)jToken, true, out var result))
            return result;

        return eDimensionIndex.Reality;
    }
}

internal class LayerTypeConverter : Il2CppJsonUnmanagedTypeConverter<LG_LayerType>
{
    protected override LG_LayerType Read(JToken jToken, LG_LayerType existingValue, JsonSerializer serializer)
    {
        switch (jToken.Type)
        {
            case JTokenType.Array:
                if (jToken is JArray arr && arr?.Count > 0)
                    return ParseEnum(arr[0]);
                return LG_LayerType.MainLayer;

            case JTokenType.Integer:
            case JTokenType.String:
                return ParseEnum(jToken);

            default:
                return LG_LayerType.MainLayer;
        }
    }

    protected override void Write(JsonWriter writer, LG_LayerType value, JsonSerializer serializer)
    {
        writer.WriteValue((byte)value);
    }

    protected override Il2CppSystem.Object ToIl2CppObject(LG_LayerType value)
    {
        return new Il2CppSystem.Byte() { m_value = (byte)value }.BoxIl2CppObject();
    }

    private static LG_LayerType ParseEnum(JToken jToken)
    {
        if (jToken.Type == JTokenType.Integer)
            return (LG_LayerType)(byte)jToken;
        
        if (jToken.Type == JTokenType.String && Enum.TryParse<LG_LayerType>((string)jToken, true, out var result))
            return result;

        return LG_LayerType.MainLayer;
    }
}

internal class LocalIndexConverter : Il2CppJsonUnmanagedTypeConverter<eLocalZoneIndex>
{
    protected override eLocalZoneIndex Read(JToken jToken, eLocalZoneIndex existingValue, JsonSerializer serializer)
    {
        switch (jToken.Type)
        {
            case JTokenType.Array:
                if (jToken is JArray arr && arr?.Count > 0)
                    return ParseEnum(arr[0]);
                return eLocalZoneIndex.Zone_0;

            case JTokenType.Integer:
            case JTokenType.String:
                return ParseEnum(jToken);

            default:
                return eLocalZoneIndex.Zone_0;
        }
    }

    protected override void Write(JsonWriter writer, eLocalZoneIndex value, JsonSerializer serializer)
    {
        writer.WriteValue((int)value);
    }

    protected override Il2CppSystem.Object ToIl2CppObject(eLocalZoneIndex value)
    {
        return new Il2CppSystem.Int32() { m_value = (int)value }.BoxIl2CppObject();
    }

    private static eLocalZoneIndex ParseEnum(JToken jToken)
    {
        if (jToken.Type == JTokenType.Integer)
            return (eLocalZoneIndex)(int)jToken;

        if (jToken.Type == JTokenType.String && Enum.TryParse<eLocalZoneIndex>((string)jToken, true, out var result))
            return result;

        return eLocalZoneIndex.Zone_0;
    }
}