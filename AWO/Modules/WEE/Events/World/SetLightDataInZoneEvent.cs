using AWO.Modules.WEE.Replicators;
using LevelGeneration;
using ModifierType = AWO.Modules.WEE.WEE_ZoneLightData.ModifierType;

namespace AWO.Modules.WEE.Events;

internal sealed class SetLightDataInZoneEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetLightDataInZone;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone)) return;

        var setting = e.SetZoneLight;
        if (!zone.gameObject.TryAndGetComponent<ZoneLightReplicator>(out var replicator))
        {
            LogError("Unable to find ZoneLightReplicator component in zone!");
            return;
        }

        /*bool shouldDisregardFlicker = setting.Type == ModifierType.SetZoneLightData ? setting.DisregardFlicker : !setting.DisregardFlicker;
        if (shouldDisregardFlicker)
        {
            foreach (var light in zone.GetComponentsInChildren<LG_Light>(true))
            {
                if (light.gameObject.TryAndGetComponent(out LG_LightAnimator flicker) && light.GetC_Light() != null && flicker.m_type == LG_LightAnimatorType.RandomFadeBlinker)
                {
                    flicker.enabled = !setting.DisregardFlicker;
                }
            }
        }*/

        if (IsMaster)
        {
            if (setting.Type == ModifierType.SetZoneLightData)
            {
                replicator.SetLightSetting(setting);
            }
            else if (setting.Type == ModifierType.RevertToOriginal)
            {
                replicator.RevertLightData();
            }
        }
    }
}
