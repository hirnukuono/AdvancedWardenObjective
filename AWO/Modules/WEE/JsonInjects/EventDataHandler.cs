using GameData;
using Il2CppJsonNet.Linq;
using InjectLib.JsonNETInjection.Handler;
using InjectLib.JsonNETInjection.Supports;

namespace AWO.Modules.WEE.JsonInjects;

internal class EventDataHandler : Il2CppJsonReferenceTypeHandler<WardenObjectiveEventData>
{
    public override void OnRead(in Il2CppSystem.Object result, in JToken jToken)
    {
        var data = result.Cast<WardenObjectiveEventData>();
        if (Enum.IsDefined((WEE_Type)data.Type))
        {
            var extData = InjectLibJSON.Deserialize<WEE_EventData>(jToken.ToString());
            data.SetWEEData(extData);
        }
    }
}

internal class TriggerDataHandler : Il2CppJsonReferenceTypeHandler<WorldEventFromSourceData>
{
    public override void OnRead(in Il2CppSystem.Object result, in JToken jToken)
    {
        var data = result.Cast<WardenObjectiveEventData>();
        if (Enum.IsDefined((WEE_Type)data.Type))
        {
            var extData = InjectLibJSON.Deserialize<WEE_EventData>(jToken.ToString());
            data.SetWEEData(extData);
        }
    }
}
