using AWO.Modules.WEE.Replicators;
using ModifierType = AWO.Modules.WEE.WEE_ZoneLightData.ModifierType;

namespace AWO.Modules.WEE.Events;

internal sealed class SetLightDataInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetLightDataInZone;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) return;

        var setting = e.SetZoneLight;
        if (!zone.gameObject.TryAndGetComponent<ZoneLightReplicator>(out var replicator))
        {
            LogError("Unable to find ZoneLightReplicator component in zone!");
            return;
        }

        switch (setting.Type)
        {
            case ModifierType.SetZoneLightData:
                if (setting.UseRandomSeed)
                {
                    setting.Seed = MasterRand.Next(int.MinValue, int.MaxValue);
                }
                replicator.SetLightSetting(setting);
                break;

            case ModifierType.RevertToOriginal:
                replicator.RevertLightData();
                break;
        }
    }
}
