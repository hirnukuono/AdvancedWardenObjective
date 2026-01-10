using GameData;
using Il2CppJsonNet;
using Il2CppJsonNet.Linq;
using InjectLib.JsonNETInjection.Converter;

namespace AWO.Modules.WEE.JsonInjects;

internal class EventTypeConverter : Il2CppJsonUnmanagedTypeConverter<eWardenObjectiveEventType>
{
    protected override eWardenObjectiveEventType Read(JToken jToken, eWardenObjectiveEventType existingValue, JsonSerializer serializer)
    {
        int value;
        switch(jToken.Type)
        {
            case JTokenType.Integer:
                value = (int)jToken;
                break;

            case JTokenType.String:
                string str = (string)jToken;
                if (Enum.TryParse<WEE_Type>(str, true, out var weeResult))
                {
                    value = (int)weeResult;
                    break;
                }
                else if (Enum.TryParse<eWardenObjectiveEventType>(str, true, out var woResult))
                {
                    value = (int)woResult;
                    break;
                }
                return eWardenObjectiveEventType.None;

            default:
                return eWardenObjectiveEventType.None;
        }

        return (eWardenObjectiveEventType)value;
    }

    protected override void Write(JsonWriter writer, eWardenObjectiveEventType value, JsonSerializer serializer)
    {
        writer.WriteValue((int)value);
    }

    protected override Il2CppSystem.Object ToIl2CppObject(eWardenObjectiveEventType value)
    {
        return new Il2CppSystem.Int32() { m_value = (int)value }.BoxIl2CppObject();
    }
}
