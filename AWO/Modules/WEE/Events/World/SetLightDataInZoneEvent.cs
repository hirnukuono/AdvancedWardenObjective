using AmorLib.Utils.Extensions;
using AWO.Modules.WEE.Replicators;
using GTFO.API;
using LevelGeneration;
using ModifierType = AWO.Modules.WEE.WEE_ZoneLightData.ModifierType;

namespace AWO.Modules.WEE.Events;

internal sealed class SetLightDataInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetLightDataInZone;
    public override bool WhitelistArrayableGlobalIndex => true;

    protected override void OnSetup()
    {
        LevelAPI.OnAfterBuildBatch += OnAfterBuildBatch;
    }

    private void OnAfterBuildBatch(LG_Factory.BatchName batch)
    {
        if (batch != LG_Factory.BatchName.ZoneLights) return;

        foreach (var zone in Builder.CurrentFloor.allZones)
        {
            zone.gameObject.AddComponent<ZoneLightReplicator>().Setup(zone);
        }
    }

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) return;

        var setting = e.SetZoneLight;
        if (!zone.gameObject.TryAndGetComponent<ZoneLightReplicator>(out var replicator))
        {
            LogError("Unable to find ZoneLightReplicator component in zone?");
            return;
        }

        switch (setting.Type)
        {
            case ModifierType.RevertToOriginal:
                replicator.RevertLightData();
                break;
            
            case ModifierType.SetZoneLightData:
                replicator.SetLightSetting(new ZoneLightState()
                {
                    lightData = setting.LightDataID,
                    lightSeed = setting.UseRandomSeed ? MasterRand.Next(int.MinValue, int.MaxValue) : setting.Seed,
                    duration = setting.TransitionDuration
                });
                break; 
        }
    }
}
