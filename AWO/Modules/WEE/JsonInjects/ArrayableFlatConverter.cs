using Il2CppJsonNet;
using Il2CppJsonNet.Linq;
using InjectLib.JsonNETInjection.Converter;

namespace AWO.Modules.WEE.JsonInjects;

internal class ArrayableFlatConverter<T> : Il2CppJsonUnmanagedTypeConverter<T> where T : unmanaged, Enum
{
    private static readonly bool IsByte = Enum.GetUnderlyingType(typeof(T)) == typeof(byte);

    protected override T Read(JToken jToken, T existingValue, JsonSerializer serializer)
    {
        switch (jToken.Type)
        {
            case JTokenType.Array:
                if (jToken is JArray arr && arr?.Count > 0)
                    return ParseEnum(arr[0]);
                return default;

            case JTokenType.Integer:
            case JTokenType.String:
                return ParseEnum(jToken);

            default:
                return default;
        }
    }

    protected override void Write(JsonWriter writer, T value, JsonSerializer serializer)
    {
        writer.WriteValue(Convert.ToInt32(value));
    }

    protected override Il2CppSystem.Object ToIl2CppObject(T value)
    {
        if (IsByte)
            return new Il2CppSystem.Byte() { m_value = Convert.ToByte(value) }.BoxIl2CppObject();

        return new Il2CppSystem.Int32() { m_value = Convert.ToInt32(value) }.BoxIl2CppObject();
    }

    private static T ParseEnum(JToken jToken)
    {
        if (jToken.Type == JTokenType.Integer)
            return (T)Enum.ToObject(typeof(T), IsByte ? (byte)jToken : (int)jToken);

        if (jToken.Type == JTokenType.String && Enum.TryParse<T>((string)jToken, true, out var result))
            return result;

        return default;
    }
}