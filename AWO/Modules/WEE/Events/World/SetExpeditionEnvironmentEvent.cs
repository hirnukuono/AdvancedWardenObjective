using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class SetExpeditionEnvironmentEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetExpeditionEnvironment;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!Dimension.GetDimension(e.DimensionIndex, out var dim) || dim.DimensionData == null)
        { 
            LogError("Dimension does not exist, or unable to find DimensionData?");
            return;
        }

        var dimData = dim.DimensionData;
        var envData = e.EnvironmentData ?? new();

        dimData.EnvironmentWetness = envData.EnvironmentWetness.GetAbsValue(dimData.EnvironmentWetness);
        dimData.DustColor = envData.UpdateColor ? envData.DustColor : dimData.DustColor;
        dimData.DustAlphaBoost = envData.DustAlphaBoost.GetAbsValue(dimData.DustAlphaBoost);
        dimData.DustTurbulence = envData.DustTurbulence.GetAbsValue(dimData.DustTurbulence);
    }
}
