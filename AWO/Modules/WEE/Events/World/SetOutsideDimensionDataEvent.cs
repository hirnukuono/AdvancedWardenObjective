using AmorLib.Events;
using AmorLib.Utils.Extensions;
using AmorLib.Utils.JsonElementConverters;
using AWO.Modules.WEE.Replicators;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class SetOutsideDimensionDataEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetOutsideDimensionData;

    private static readonly Func<WEE_SetOutsideDimensionData, ValueBase>[] _baseFields =
    {
        d => d.LightAzimuth,
        d => d.LightElevation,
        d => d.LightIntensity,
        d => d.AmbientIntensity,
        d => d.ReflectionsIntensity,
        d => d.GodrayRange,
        d => d.GodrayExponent,
        d => d.AtmosphereDensity,
        d => d.Exposure,
        d => d.AerialScale,
        d => d.MieScattering, 
        d => d.MieG,         
        d => d.MultipleScattering, 
        d => d.CloudsCoverage,     
        d => d.CloudsDensity,      
        d => d.CloudsSharpness,   
        d => d.CloudsShadowOpacity,
        d => d.CloudsTimescale,    
        d => d.CloudsCrawling,     
        d => d.CloudsFade,         
        d => d.SandstormEdgeA,     
        d => d.SandstormEdgeB,     
        d => d.SandstormMinFog,    
    };

    protected override void OnSetup()
    {
        LevelEvents.OnBuildDoneLate += OnBuildDoneLate;
    }

    private void OnBuildDoneLate()
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
            LogError("Dimension does not exist, or unable to find enabled OutsideDataReplicator in dimension?");
            return;
        }

        var dimData = e.DimensionData ?? new();
        var state = new OutsideDataState()
        {
            duration = e.Duration,
            revertToOriginal = !e.Enabled,
            isOutside = dimData.IsOutside,
            atmosphereData = dimData.AtmosphereData,
            cloudsData = dimData.CloudsData,
            sandstorm = dimData.Sandstorm.GetValue(replicator.OutsideData.Sandstorm),
        };
        unsafe { PackFields(dimData, replicator.OutsideData, state.fieldData); }
        replicator.SetOutsideData(state);
    }

    private static unsafe void PackFields(WEE_SetOutsideDimensionData d, DimensionData r, float* dest)
    {
        for (int i = 0; i < _baseFields.Length; i++)
        {
            ValueBase field = _baseFields[i](d); 
            float orig = OutsideDataReplicator.FieldMap[i].Get(r); 
            dest[i] = (field.Mode == ValueMode.Rel && field.Value == 1) ? float.NaN : field.GetAbsValue(orig);
        }
    }    
}
