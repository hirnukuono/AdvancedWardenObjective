using AWO.CustomFields;
using AWO.Jsons.ManagedJson;
using AWO.Modules.WEE;
using GameData;
using Il2CppJsonNet.Linq;
using InjectLib.FieldInjection;
using InjectLib.JsonNETInjection.Supports;
using InjectLib.JsonNETInjection.Handler;
using System;
using System.Text.Json;
using JSON = GTFO.API.JSON.JsonSerializer;

namespace AWO.WEE.JsonInjects;
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
