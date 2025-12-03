using AmorLib.Utils.Extensions;
using AWO.Modules.WEE.Replicators;
using GTFO.API;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class SetOutsideDimensionDataEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetOutsideDimensionData;

    protected override void OnSetup()
    {
        LevelAPI.OnFactoryDone += OnFactoryDone;
    }

    private void OnFactoryDone()
    {
        foreach (var dim in Builder.CurrentFloor.m_dimensions)
        {
            dim.DimensionLevel.gameObject.AddComponent<OutsideDataReplicator>().Setup(dim);
        }
    }

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!Dimension.GetDimension(e.DimensionIndex, out var dim) || !dim.DimensionLevel.gameObject.TryAndGetComponent<OutsideDataReplicator>(out var replicator) || !replicator.enabled)
        {
            LogError("Dimension does not exist, or unable to find enabled OutsideDataReplicator in dimension");
            return;
        }

        var dimData = e.DimensionData;
        replicator.SetOutsideData(new OutsideDataState()
        {
            duration = e.Duration,
            revertToOriginal = !e.Enabled,
            isOutside = dimData.IsOutside,
            atmosphereData = dimData.AtmosphereData,
            cloudsData = dimData.CloudsData,
            sandstorm = dimData.Sandstorm.GetValue(replicator.OutsideData!.Sandstorm),
            fieldData = PackFields(dimData, replicator)
        });
    }

    private static float[] PackFields(WEE_SetOutsideDimensionData d, OutsideDataReplicator replicator)
    {
        var r = replicator.OutsideData!;
        return new float[]
        {
            d.LightAzimuth.GetAbsValue(r.LightAzimuth),
            d.LightElevation.GetAbsValue(r.LightElevation),
            d.LightIntensity.GetAbsValue(r.LightIntensity),
            d.AmbientIntensity.GetAbsValue(r.AmbientIntensity),
            d.ReflectionsIntensity.GetAbsValue(r.ReflectionsIntensity),
            d.GodrayRange.GetAbsValue(r.GodrayRange),
            d.GodrayExponent.GetAbsValue(r.GodrayExponent),
            d.AtmosphereDensity.GetAbsValue(r.AtmosphereDensity),
            d.Exposure.GetAbsValue(r.Exposure),
            d.AerialScale.GetAbsValue(r.AerialScale),
            d.MieScattering.GetAbsValue(r.MieScattering),
            d.MieG.GetAbsValue(r.MieG),
            d.MultipleScattering.GetAbsValue(r.MultipleScattering),
            d.CloudsCoverage.GetAbsValue(r.CloudsCoverage),
            d.CloudsDensity.GetAbsValue(r.CloudsDensity),
            d.CloudsSharpness.GetAbsValue(r.CloudsSharpness),
            d.CloudsShadowOpacity.GetAbsValue(r.CloudsShadowOpacity),
            d.CloudsTimescale.GetAbsValue(r.CloudsTimescale),
            d.CloudsCrawling.GetAbsValue(r.CloudsCrawling),
            d.CloudsFade.GetAbsValue(r.CloudsFade),
            d.SandstormEdgeA.GetAbsValue(r.SandstormEdgeA),
            d.SandstormEdgeB.GetAbsValue(r.SandstormEdgeB),
            d.SandstormMinFog.GetAbsValue(r.SandstormMinFog)
        };
    }
}
