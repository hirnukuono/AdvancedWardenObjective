using GameData;
using Il2CppJsonNet.Linq;
using InjectLib.JsonNETInjection.Handler;

namespace AWO.Modules.WOE.JsonInjects;

[Obsolete]
internal class ObjectiveDataHandler : Il2CppJsonReferenceTypeHandler<WardenObjectiveDataBlock>
{
    public override void OnRead(in Il2CppSystem.Object result, in JToken jToken)
    {
        if (jToken.Type != JTokenType.Object)
        {
            return;
        }

        var jObj = (JObject)jToken;
        if (!jObj.TryGetValue("woeEnabled", out var resultToken))
        {
            return;
        }

        if (resultToken.Type != JTokenType.Boolean)
        {
            return;
        }

        if ((bool)resultToken == true)
        {
            var db = result.Cast<WardenObjectiveDataBlock>();
            
        }
    }
}
