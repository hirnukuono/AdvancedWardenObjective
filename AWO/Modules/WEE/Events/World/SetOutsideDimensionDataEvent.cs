using AmorLib.Utils.Extensions;
using AmorLib.Utils.JsonElementConverters;
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

        var dimData = e.DimensionData ?? new();
        replicator.SetOutsideData(new OutsideDataState()
        {
            duration = e.Duration,
            revertToOriginal = !e.Enabled,
            isOutside = dimData.IsOutside,
            atmosphereData = dimData.AtmosphereData,
            cloudsData = dimData.CloudsData,
            sandstorm = dimData.Sandstorm.GetValue(replicator.OutsideData.Sandstorm),
            fieldData = PackFields(dimData, replicator)
        });
    }

    private static float[] PackFields(WEE_SetOutsideDimensionData d, OutsideDataReplicator replicator)
    {
        var r = replicator.OutsideData;
        return new float[]
        {
            Pack(d.LightAzimuth, r.LightAzimuth),
            Pack(d.LightElevation, r.LightElevation),
            Pack(d.LightIntensity, r.LightIntensity),
            Pack(d.AmbientIntensity, r.AmbientIntensity),
            Pack(d.ReflectionsIntensity, r.ReflectionsIntensity),
            Pack(d.GodrayRange, r.GodrayRange),
            Pack(d.GodrayExponent, r.GodrayExponent),
            Pack(d.AtmosphereDensity, r.AtmosphereDensity),
            Pack(d.Exposure, r.Exposure),
            Pack(d.AerialScale, r.AerialScale),
            Pack(d.MieScattering, r.MieScattering),
            Pack(d.MieG, r.MieG),
            Pack(d.MultipleScattering, r.MultipleScattering),
            Pack(d.CloudsCoverage, r.CloudsCoverage),
            Pack(d.CloudsDensity, r.CloudsDensity),
            Pack(d.CloudsSharpness, r.CloudsSharpness),
            Pack(d.CloudsShadowOpacity, r.CloudsShadowOpacity),
            Pack(d.CloudsTimescale, r.CloudsTimescale),
            Pack(d.CloudsCrawling, r.CloudsCrawling),
            Pack(d.CloudsFade, r.CloudsFade),
            Pack(d.SandstormEdgeA, r.SandstormEdgeA),
            Pack(d.SandstormEdgeB, r.SandstormEdgeB),
            Pack(d.SandstormMinFog, r.SandstormMinFog)
        };
        
        static float Pack(ValueBase d, float r)
        {
            return (d.Mode == ValueMode.Rel && d.Value == 1) ? float.NaN : d.GetAbsValue(r);
        }
    }    
}
