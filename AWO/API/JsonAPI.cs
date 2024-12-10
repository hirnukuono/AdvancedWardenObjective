using AWO.Modules.WEE.JsonInjects;
using System.Text.Json.Serialization;

namespace AWO.API;

[Obsolete]
public static class JsonAPI
{
    //INFO: Legacy, Use InjectLibConnector from InjectLib
    //public static JsonConverter EventDataConverter => new ManagedEventDataConverter();
}
