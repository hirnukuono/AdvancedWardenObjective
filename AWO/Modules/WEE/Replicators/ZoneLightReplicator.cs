using AmorLib.API;
using AmorLib.Networking.StateReplicators;
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
    public LG_Zone? Zone;
    public LightWorker[] LightsInZone = Array.Empty<LightWorker>();
    private readonly Dictionary<ILightModifier, Coroutine?> _lightMods = new();

    public void Setup(LG_Zone zone)
    {
        Replicator = StateReplicator<ZoneLightState>.Create((uint)zone.ID + 1 /*Zone ID can be start with 0*/, new()
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
                    Zone?.StopCoroutine(kvp.Value);
                kvp.Key.Remove();
            }
            _lightMods.Clear();
            LightsInZone.ForEachWorker(worker => worker.ToggleLightFlicker(true));
            return;
        }
        
        var block = LightSettingsDataBlock.GetBlock(state.lightData);
        if (block == null || !block.internalEnabled)
        {
            Logger.Error($"Failed to find enabled LightSettingsDataBlock {state.lightData}!");
            return;
        }

        System.Random rand = new(state.lightSeed);
        for (int i = 0; i < LightsInZone.Length; i++, rand.Next())
        {
            ApplyLightMod(block, isRecall ? 0.0f : state.duration, rand, i); // set new light settings
        }
    }

    [HideFromIl2Cpp]
    private void ApplyLightMod(LightSettingsDataBlock lightDB, float duration, System.Random rand, int ixseed)
    {
        var worker = LightsInZone[ixseed];
        var light = worker.Light;
        var mod = worker.AddModifier(light.m_color, light.m_intensity, light.enabled);

        if (_lightMods.TryGetValue(mod, out var oldC) && oldC != null)
        {
            Zone?.StopCoroutine(oldC);
        }
        _lightMods[mod] = null;        
        
        var selector = new LightSettingSelector();
        selector.Setup(worker.Light.m_category, lightDB);
        if (selector.TryGetRandomSetting((uint)ixseed, out var setting))
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
                    startIntensity = light.m_intensity,
                    endIntensity = worker.PrefabIntensity * setting.IntensityMul,
                    startColor = light.m_color,
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
        var mod = data.lightMod;

        while (time <= data.duration)
        {
            time += Time.deltaTime;
            progress = Mathf.Clamp01(time / data.duration);
            mod.Color = Color.Lerp(data.startColor, data.endColor, progress);
            mod.Intensity = Mathf.Lerp(data.startIntensity, data.endIntensity, progress);
            yield return null;
        }

        switch (data.endMode)
        {
            case LightTransitionData.Mode.Enabled:
                mod.Enabled = true;
                break;

            case LightTransitionData.Mode.Disabled:
                mod.Enabled = false;
                break;

            case LightTransitionData.Mode.Flickering:
                mod.Enabled = true;
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
}
