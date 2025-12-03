using AWO.Modules.WEE;
using GameData;
using InjectLib.FieldInjection;

namespace AWO.CustomFields;

internal static class WOEventDataFields
{
    internal static void Init()
    {
        FieldInjector<WardenObjectiveEventData>.DefineManagedField<WEE_EventData>("m_WEEDataRef");
    }

    public static void SetWEEData(this WardenObjectiveEventData target, WEE_EventData data)
    {
        FieldInjector<WardenObjectiveEventData>.TrySetManagedField(target, "m_WEEDataRef", data);
    }

    public static WEE_EventData GetWEEData(this WardenObjectiveEventData target)
    {
        FieldInjector<WardenObjectiveEventData>.TryGetManagedField<WEE_EventData>(target, "m_WEEDataRef", out var data);
        return data;
    }
}
