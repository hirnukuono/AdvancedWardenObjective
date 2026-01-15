using AmorLib.Networking.StateReplicators;
using BepInEx.Unity.IL2CPP.Utils;
using LevelGeneration;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Replicators;

public unsafe struct OutsideDataState
{
    public float duration;
    public bool revertToOriginal;
    public bool isOutside;
    public uint atmosphereData;
    public uint cloudsData;
    public bool sandstorm;
    public fixed float fieldData[23];
}

public sealed class OutsideDataReplicator : MonoBehaviour, IStateReplicatorHolder<OutsideDataState>
{
    [HideFromIl2Cpp]
    public StateReplicator<OutsideDataState>? Replicator { get; private set; }
    public LG_Dimension Dimension = null!;
    public DimensionData OutsideData = null!;
    private OutsideDataState _origData;
    private float[] _origFields = new float[FieldMap.Length];
    private Coroutine? _transitionCoroutine;

    internal static readonly (Func<DimensionData, float> Get, Action<DimensionData, float> Set)[] FieldMap =
    {
        (d => d.LightAzimuth,        (d,v) => d.LightAzimuth = v),
        (d => d.LightElevation,      (d,v) => d.LightElevation = v),
        (d => d.LightIntensity,      (d,v) => d.LightIntensity = v),
        (d => d.AmbientIntensity,    (d,v) => d.AmbientIntensity = v),
        (d => d.ReflectionsIntensity,(d,v) => d.ReflectionsIntensity = v),
        (d => d.GodrayRange,         (d,v) => d.GodrayRange = v),
        (d => d.GodrayExponent,      (d,v) => d.GodrayExponent = v),
        (d => d.AtmosphereDensity,   (d,v) => d.AtmosphereDensity = v),
        (d => d.Exposure,            (d,v) => d.Exposure = v),
        (d => d.AerialScale,         (d,v) => d.AerialScale = v),
        (d => d.MieScattering,       (d,v) => d.MieScattering = v),
        (d => d.MieG,                (d,v) => d.MieG = v),
        (d => d.MultipleScattering,  (d,v) => d.MultipleScattering = v),
        (d => d.CloudsCoverage,      (d,v) => d.CloudsCoverage = v),
        (d => d.CloudsDensity,       (d,v) => d.CloudsDensity = v),
        (d => d.CloudsSharpness,     (d,v) => d.CloudsSharpness = v),
        (d => d.CloudsShadowOpacity, (d,v) => d.CloudsShadowOpacity = v),
        (d => d.CloudsTimescale,     (d,v) => d.CloudsTimescale = v),
        (d => d.CloudsCrawling,      (d,v) => d.CloudsCrawling = v),
        (d => d.CloudsFade,          (d,v) => d.CloudsFade = v),
        (d => d.SandstormEdgeA,      (d,v) => d.SandstormEdgeA = v),
        (d => d.SandstormEdgeB,      (d,v) => d.SandstormEdgeB = v),
        (d => d.SandstormMinFog,     (d,v) => d.SandstormMinFog = v)
    };

    private static float[] GetFieldArray(DimensionData dimData)
    {
        float[] fields = new float[FieldMap.Length];
        int i = 0;
        foreach (var (Get, _) in FieldMap)
        {
            fields[i++] = Get(dimData);
        }
        return fields;
    }

    public void Setup(Dimension dim)
    {
        Dimension = GetComponent<LG_Dimension>();

        if (dim.DimensionData == null)
        {
            enabled = false;
            return;
        }

        OutsideData = dim.DimensionData;
        _origData = new()
        {
            revertToOriginal = true,
            isOutside = OutsideData.IsOutside,
            atmosphereData = OutsideData.AtmosphereData,
            cloudsData = OutsideData.CloudsData,
            sandstorm = OutsideData.Sandstorm,
        };
        _origFields = GetFieldArray(OutsideData);
        
        Replicator = StateReplicator<OutsideDataState>.Create((uint)dim.DimensionIndex + 1, _origData, LifeTimeType.Session, this);
    }

    public void OnDestroy()
    {
        Replicator?.Unload();
    }

    public void SetOutsideData(OutsideDataState data)
    {
        Replicator?.SetState(data);
    }

    public void OnStateChange(OutsideDataState oldState, OutsideDataState state, bool isRecall)
    {
        if (_transitionCoroutine != null)
        {
            Dimension.StopCoroutine(_transitionCoroutine);
        }

        float duration = isRecall ? 0f : state.duration;
        float[] fieldArr = new float[FieldMap.Length];
        if (state.revertToOriginal)
        {
            state = _origData;
            fieldArr = _origFields;
        }
        else
        {
            unsafe
            {
                float* src = state.fieldData;
                for (int i = 0; i < FieldMap.Length; i++)
                { 
                    fieldArr[i] = src[i]; 
                }
            }
        }

        _transitionCoroutine = Dimension.StartCoroutine(OutsideTransition(state with { duration = duration }, fieldArr));
    }

    [HideFromIl2Cpp]
    private IEnumerator OutsideTransition(OutsideDataState data, float[] fieldArr)
    {
        OutsideData.IsOutside = data.isOutside;
        OutsideData.AtmosphereData = data.atmosphereData != 0 ? data.atmosphereData : OutsideData.AtmosphereData;
        OutsideData.CloudsData = data.cloudsData != 0 ? data.cloudsData : OutsideData.CloudsData;
        OutsideData.Sandstorm = data.sandstorm;

        float[] startValues = GetFieldArray(OutsideData);
        float startAzimuth = startValues[0];
        float startElevation = startValues[1];
        float endAzimuth = fieldArr[0];
        float endElevation = fieldArr[1];
        bool azimuthOnly = !float.IsNaN(endAzimuth);
        bool elevationOnly = !float.IsNaN(endElevation);

        Vector3 startDir = Vector3.zero;
        Vector3 endDir = Vector3.zero;
        bool slerpFlag = azimuthOnly && elevationOnly;
        if (slerpFlag)
        {
            startDir = DirFromDeg(startAzimuth, startElevation);
            endDir = DirFromDeg(endAzimuth, endElevation);
        }

        float time = 0f;
        while (time <= data.duration)
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / data.duration);

            if (slerpFlag)
            {
                Vector3 dir = Vector3.Slerp(startDir, endDir, progress).normalized;
                float elDeg = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
                float azDeg = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; // 0 = +Z, positive toward +X
                FieldMap[0].Set(OutsideData, azDeg);
                FieldMap[1].Set(OutsideData, elDeg);
            }
            else if (azimuthOnly)
            {
                float azimuth = Mathf.LerpAngle(startAzimuth, endAzimuth, progress);
                FieldMap[0].Set(OutsideData, azimuth);
            }
            else if (elevationOnly)
            {
                float elevation = Mathf.Lerp(startElevation, endElevation, progress);
                FieldMap[1].Set(OutsideData, elevation);
            }

            for (int i = 2; i < FieldMap.Length; i++)
            {
                float endValue = fieldArr[i];
                if (float.IsNaN(endValue)) continue;
                float value = Mathf.Lerp(startValues[i], endValue, progress);
                FieldMap[i].Set(OutsideData, value);
            }

            yield return null;
        }

        Vector3 DirFromDeg(float azDeg, float elDeg)
        {
            float az = azDeg * Mathf.Deg2Rad;
            float el = elDeg * Mathf.Deg2Rad;
            float y = Mathf.Sin(el);
            float r = Mathf.Cos(el);
            float x = Mathf.Sin(az) * r;
            float z = Mathf.Cos(az) * r;
            return new Vector3(x, y, z).normalized;
        }
    }
}