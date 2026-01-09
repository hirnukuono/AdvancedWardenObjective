using AmorLib.API;
using AmorLib.Networking.StateReplicators;
using AmorLib.Utils;
using BepInEx.Unity.IL2CPP.Utils;
using GameData;
using LevelGeneration;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Replicators;

public struct ZoneLightState
{
    public uint lightData;
    public int lightSeed;
    public float duration;
}

public struct LightTransitionData
{
    public ILightModifier lightMod;
    public float duration;
    public float origIntensity;
    public float startIntensity;
    public float endIntensity;
    public Color startColor;
    public Color endColor;
    public Mode endMode;
    public int endModeSeed;

    public enum Mode
    {
        Enabled,
        Disabled,
        Flickering
    }
}

public sealed class ZoneLightReplicator : MonoBehaviour, IStateReplicatorHolder<ZoneLightState>
{
    [HideFromIl2Cpp]
    public StateReplicator<ZoneLightState>? Replicator { get; private set; }
    public LG_Zone Zone = null!;
    public LightWorker[] LightsInZone = Array.Empty<LightWorker>();
    private readonly Dictionary<ILightModifier, Coroutine?> _lightMods = new();

    public void Setup(LG_Zone zone)
    {
        Replicator = StateReplicator<ZoneLightState>.Create((uint)zone.ID + 1, new()
        {
            lightData = 0u
        }, LifeTimeType.Session, this);

        LightsInZone = LightAPI.GetLightWorkersInZone(zone).ToArray();
        Zone = zone;

        if (OnLightsChanged != null)
        {
            OnSharedStatus = OnLightsChanged;
        }
    }

    public void OnDestroy()
    {
        Replicator?.Unload();
    }

    public void SetLightSetting(ZoneLightState data)
    {
        Replicator?.SetState(data);        
    }

    public void RevertLightData()
    {
        Replicator?.SetState(new ZoneLightState() { lightData = 0u });        
    }
    
    public void OnStateChange(ZoneLightState oldState, ZoneLightState state, bool isRecall)
    {
        if (state.lightData == 0u) // revert light settings
        {
            foreach(var kvp in _lightMods)
            {
                if (kvp.Value != null) 
                    Zone.StopCoroutine(kvp.Value);
                kvp.Key.Remove();
            }
            _lightMods.Clear();
            LightsInZone.ForEachWorker(worker => worker.ToggleLightFlicker(true));

            StopShareStatus();
            ShareStatus();
            return;
        }

        if (!DataBlockUtil.TryGetBlock<LightSettingsDataBlock>(state.lightData, out var block))
        {
            Logger.Error($"Failed to find enabled LightSettingsDataBlock {state.lightData}!");
            return;
        }
        
        for (int i = 0; i < LightsInZone.Length; i++)
        {
            ApplyLightMod(block, isRecall ? 0.0f : state.duration, state.lightSeed, i); // set new light settings
        }
        StartShareStatus(state.duration);
    }

    private void ApplyLightMod(LightSettingsDataBlock lightDB, float duration, int seed, int subseed)
    {
        System.Random rand = new(seed);
        for (int i = 0; i < Mathf.Abs(subseed); i++)
        {
            rand.Next();
        }

        var worker = LightsInZone[subseed];
        var light = worker.Light;
        var mod = worker.AddModifier(light.m_color, light.m_intensity, light.enabled);

        if (_lightMods.TryGetValue(mod, out var coroutine) && coroutine != null)
        {
            Zone.StopCoroutine(coroutine);
        }
        _lightMods[mod] = null;        
        
        var selector = new LightSettingSelector();
        selector.Setup(worker.Light.m_category, lightDB);
        if (selector.TryGetRandomSetting((uint)subseed, out var setting))
        {
            worker.ToggleLightFlicker(false);

            if (!rand.MeetProbability(setting.Chance)) // light ends disabled
            {
                _lightMods[mod] = Zone.StartCoroutine(LightTransition(new()
                {
                    lightMod = mod,
                    duration = duration,
                    origIntensity = worker.PrefabIntensity,
                    startIntensity = mod.Intensity,
                    endIntensity = 0.0f,
                    startColor = mod.Color,
                    endColor = Color.black,
                    endMode = LightTransitionData.Mode.Disabled
                }));
            }
            else // light ends enabled or flickering
            {
                _lightMods[mod] = Zone.StartCoroutine(LightTransition(new()
                {
                    lightMod = mod,
                    duration = duration,
                    origIntensity = worker.PrefabIntensity,
                    startIntensity = mod.Enabled ? mod.Intensity : 0.0f,
                    endIntensity = worker.PrefabIntensity * setting.IntensityMul,
                    startColor = mod.Enabled ? mod.Color : Color.black,
                    endColor = setting.Color,
                    endMode = rand.MeetProbability(setting.ChanceBroken) ? LightTransitionData.Mode.Flickering : LightTransitionData.Mode.Enabled,
                    endModeSeed = rand.Next()
                }));
            }
        }
    }

    [HideFromIl2Cpp]
    private IEnumerator LightTransition(LightTransitionData data)
    {        
        float time = 0.0f;
        float progress;
        bool flag = false;
        var mod = data.lightMod;

        while (time <= data.duration)
        {
            time += Time.deltaTime;
            progress = Mathf.Clamp01(time / data.duration);
            mod.Color = Color.Lerp(data.startColor, data.endColor, progress);
            mod.Intensity = Mathf.Lerp(data.startIntensity, data.endIntensity, progress);
            yield return null;

            if (!flag && data.endMode != LightTransitionData.Mode.Disabled)
            {
                flag = true;
                mod.Enabled = true;
            }
        }

        switch (data.endMode)
        {
            case LightTransitionData.Mode.Disabled:
                mod.Enabled = false;
                break;

            case LightTransitionData.Mode.Flickering:
                _lightMods[data.lightMod] = Zone.StartCoroutine(LightAnimation(data));
                break;
        }
    }

    [HideFromIl2Cpp]
    private IEnumerator LightAnimation(LightTransitionData data)
    {
        float time, duration, delay, ramp;
        var rand = new System.Random(data.endModeSeed);
        var mod = data.lightMod;

        while (true)
        {
            time = 0.0f;
            duration = rand.NextRange(0.1f, 2.5f);
            delay = rand.NextRange(0.2f, 1.0f);

            while (time <= duration)
            {
                time += Time.deltaTime;
                ramp = Mathf.Clamp01(time / duration);
                mod.Intensity = data.origIntensity * ramp * ramp;
                yield return null;
            }

            mod.Intensity = 0.0f;
            yield return new WaitForSeconds(delay);
        }
    }

    /* remove this stuff later */
    [HideFromIl2Cpp]
    public event Action? OnSharedStatus;
    [HideFromIl2Cpp]
    public event Action? OnLightsChanged;
    public Coroutine? ShareStatusCoroutine;

    public void ShareStatus()
    {
        OnSharedStatus?.Invoke();
    }

    public void StartShareStatus(float duration)
    {
        if (OnSharedStatus != null)
        {
            StopShareStatus();
            ShareStatusCoroutine = CoroutineManager.StartCoroutine(DoShareStatus(duration).WrapToIl2Cpp());
        }
    }

    public void StopShareStatus()
    {
        if (ShareStatusCoroutine != null)
        {
            CoroutineManager.StopCoroutine(ShareStatusCoroutine);
            ShareStatusCoroutine = null;
        }
    }

    [HideFromIl2Cpp]
    private IEnumerator DoShareStatus(float duration)
    {
        float time = 0.0f;
        float interval = GetInvocationInterval(duration);
        float nextInvoke = interval;
        bool shouldSync = !float.IsNaN(interval);

        while (time <= duration)
        {
            time += Time.fixedDeltaTime;
            if (shouldSync && nextInvoke <= time && nextInvoke < duration)
            {
                ShareStatus();
                nextInvoke += interval;
            }
            yield return null;
        }

        ShareStatus();
    }

    private static float GetInvocationInterval(float time) // a very overcomplicated way to get faster zone light change sync intervals
    {
        if (time < 2.0f)
        {
            return float.NaN;
        }
        else if (time < 10.0f)
        {
            return time / 2.0f;
        }

        int timef = (int)Math.Floor(time);
        if (timef.IsPrime())
        {
            timef -= 1;
        }

        List<int> divisors = new();
        int rad = (int)Math.Sqrt(timef);
        for (int i = 1; i <= rad; i++)
        {
            if (timef % i == 0)
            {
                divisors.Add(i);
                int pair = timef / i;
                if (pair != i)
                {
                    divisors.Add(pair);
                }
            }
        }
        divisors.Sort();
        var inner = divisors.Skip(1).Take(divisors.Count - 2).ToList();
        if (inner.Count == 0)
        {
            return float.NaN;
        }

        int interval;
        float mean = inner.Sum() / (float)inner.Count;
        if (inner.Count % 2 == 1)
        {
            interval = (int)mean;
        }
        else
        {
            interval = inner.Aggregate((a, b) =>
            {
                float da = Math.Abs(a - mean);
                float db = Math.Abs(b - mean);
                if (da == db) return Math.Max(a, b);
                return da < db ? a : b;
            });
        }

        return Math.Min(interval, 30.0f);
    }
}
