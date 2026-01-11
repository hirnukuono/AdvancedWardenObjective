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

public sealed partial class ZoneLightReplicator : MonoBehaviour, IStateReplicatorHolder<ZoneLightState>
{
    [HideFromIl2Cpp]
    public StateReplicator<ZoneLightState>? Replicator { get; private set; }
    public LG_Zone Zone = null!;
    public LightWorker[] LightsInZone = Array.Empty<LightWorker>();
    private readonly Dictionary<ILightModifier, Coroutine?> _lightMods = new();
    private readonly Dictionary<int, ILightModifier> _modMap = new();

    public void Setup(LG_Zone zone)
    {
        Replicator = StateReplicator<ZoneLightState>.Create((uint)zone.ID + 1, new()
        {
            lightData = 0u
        }, LifeTimeType.Session, this);

        LightsInZone = LightAPI.GetLightWorkersInZone(zone).ToArray();
        Zone = zone;
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
            _modMap.Clear();
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
            ApplyLightMod(block, isRecall ? 0f : state.duration, state.lightSeed, i); // set new light settings
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

        if (_modMap.TryGetValue(worker.InstanceID, out var oldMod) && _lightMods.TryGetValue(oldMod, out var coroutine) && coroutine != null)
        {
            Zone.StopCoroutine(coroutine);
            oldMod.Remove();
        }
        _lightMods[mod] = null; 
        _modMap[worker.InstanceID] = mod;

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
                    endIntensity = 0f,
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
                    startIntensity = mod.Enabled ? mod.Intensity : 0f,
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
        float time = 0f;
        float progress;
        bool flag = false;
        var mod = data.lightMod;

        if (!mod.Enabled && data.endMode != LightTransitionData.Mode.Disabled)
        {
            mod.Color = Color.black;
            mod.Intensity = 0f;
        }

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
            time = 0f;
            duration = rand.NextRange(0.1f, 2.5f);
            delay = rand.NextRange(0.2f, 1f);

            while (time <= duration)
            {
                time += Time.deltaTime;
                ramp = Mathf.Clamp01(time / duration);
                mod.Intensity = data.origIntensity * ramp * ramp;
                yield return null;
            }

            mod.Intensity = 0f;
            yield return new WaitForSeconds(delay);
        }
    }
}
