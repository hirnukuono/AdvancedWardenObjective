using AmorLib.Networking.StateReplicators;
using LevelGeneration;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Replicators;

public struct OutsideDataState
{
    public float duration;
    public bool revertToOriginal;
    public bool isOutside;
    public uint atmosphereData;
    public uint cloudsData;
    public bool sandstorm;
    public float[] fieldData;
}

public sealed class OutsideDataReplicator : MonoBehaviour, IStateReplicatorHolder<OutsideDataState>
{
    [HideFromIl2Cpp]
    public StateReplicator<OutsideDataState>? Replicator { get; private set; }
    public LG_Dimension Dimension = null!;
    public DimensionData OutsideData = null!;
    private OutsideDataState _origData;
    private Coroutine? _transitionCoroutine;

    private static readonly (Func<DimensionData, float> Get, Action<DimensionData, float> Set)[] _fieldMap =
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
        float[] fields = new float[_fieldMap.Length];
        int i = 0;
        foreach (var (Get, Set) in _fieldMap)
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
            fieldData = GetFieldArray(OutsideData)
        };
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
            Dimension.StopCoroutine(_transitionCoroutine);

        _transitionCoroutine = Dimension.StartCoroutine(OutsideTransition(state, isRecall).WrapToIl2Cpp());
    }

    [HideFromIl2Cpp]
    private IEnumerator OutsideTransition(OutsideDataState data, bool isRecall)
    {
        if (data.fieldData?.Length != _fieldMap.Length)
        {
            Logger.Error("OutsideDataReplicator", "Received fieldData does not match, aborting!");
            yield break;
        }

        float duration = isRecall ? 0f : data.duration;
        data = data.revertToOriginal ? _origData : data;
        OutsideData.IsOutside = data.isOutside;
        OutsideData.AtmosphereData = data.atmosphereData != 0 ? data.atmosphereData : OutsideData.AtmosphereData;
        OutsideData.CloudsData = data.cloudsData != 0 ? data.cloudsData : OutsideData.CloudsData;
        OutsideData.Sandstorm = data.sandstorm;

        float[] startValues = GetFieldArray(OutsideData);
        float startAzimuth = startValues[0];
        float startElevation = startValues[1];
        float endAzimuth = data.fieldData[0];
        float endElevation = data.fieldData[1];
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
        while (time <= duration)
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / duration);

            if (slerpFlag)
            {
                Vector3 dir = Vector3.Slerp(startDir, endDir, progress).normalized;
                float elDeg = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
                float azDeg = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; // 0 = +Z, positive toward +X
                _fieldMap[0].Set(OutsideData, azDeg);
                _fieldMap[1].Set(OutsideData, elDeg);
            }
            else if (azimuthOnly)
            {
                float azimuth = Mathf.LerpAngle(startAzimuth, endAzimuth, progress);
                _fieldMap[0].Set(OutsideData, azimuth);
            }
            else if (elevationOnly)
            {
                float elevation = Mathf.Lerp(startElevation, endElevation, progress);
                _fieldMap[1].Set(OutsideData, elevation);
            }

            for (int i = 2; i < _fieldMap.Length; i++)
            {
                float endValue = data.fieldData[i];
                if (float.IsNaN(endValue)) continue;
                float value = Mathf.Lerp(startValues[i], endValue, progress);
                _fieldMap[i].Set(OutsideData, value);
            }

            yield return null;
        }

        static Vector3 DirFromDeg(float azDeg, float elDeg)
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
