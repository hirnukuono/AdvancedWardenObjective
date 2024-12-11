using AWO.API;
using AWO.Networking;
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

public class LightWorker
{
    public LG_Zone OwnerZone;
    public LG_Light Light;
    public int InstanceID;
    public Color OrigColor; 
    public bool OrigEnabled;
    public float PrefabIntensity;
    public float OrigIntensity;
    public Coroutine LightAnimationRoutine;
    public Coroutine LightTransitionRoutine;
    //public bool ExcludeFromZoneLightJob = false;

    public void ApplyLightSetting(LightSettingsDataBlock lightDB, float duration, int seed, int subseed)
    {
        var rand = new System.Random(seed);
        for (int i = 0; i < Mathf.Abs(subseed); i++)
        {
            rand.Next();
        }

        var selector = new LightSettingSelector();
        selector.Setup(Light.m_category, lightDB);
        if (selector.TryGetRandomSetting((uint)subseed, out var setting))
        {
            if (!rand.MeetProbability(setting.Chance))
            {
                LightTransitionRoutine = OwnerZone.StartCoroutine(LightTransition(new()
                {
                    startColor = Light.m_color,
                    endColor = Color.black,
                    startIntensity = Light.m_intensity,
                    endIntensity = 0.0f,
                    endMode = LightTransitionData.Mode.Disabled
                }, duration));
            }
            else
            {
                var mode = (rand.MeetProbability(setting.ChanceBroken))
                    ? LightTransitionData.Mode.Flickering : LightTransitionData.Mode.Enabled;

                LightTransitionRoutine = OwnerZone.StartCoroutine(LightTransition(new()
                {
                    startColor = Light.m_color,
                    endColor = setting.Color,
                    startIntensity = Light.m_intensity,
                    endIntensity = PrefabIntensity * setting.IntensityMul,
                    endMode = mode,
                    endModeSeed = rand.Next()
                }, duration));
            }
        }
    }

    private IEnumerator LightTransition(LightTransitionData data, float duration)
    {
        float time = 0.0f;
        var yielder = new WaitForFixedUpdate();
        while (time <= duration)
        {
            time += Time.fixedDeltaTime;

            float progress = time / duration;
            Light.ChangeColor(Color.Lerp(data.startColor, data.endColor, progress));
            Light.ChangeIntensity(Mathf.Lerp(data.startIntensity, data.endIntensity, progress));
            yield return yielder;
        }

        StopAnimation();

        switch (data.endMode)
        {
            case LightTransitionData.Mode.Enabled:
                Light.SetEnabled(true);
                break;

            case LightTransitionData.Mode.Disabled:
                Light.SetEnabled(false);
                break;

            case LightTransitionData.Mode.Flickering:
                Light.SetEnabled(true);
                LightAnimationRoutine = OwnerZone.StartCoroutine(LightAnimation(data.endModeSeed));
                break;
        }
    }

    private IEnumerator LightAnimation(int seed)
    {
        var rand = new System.Random(seed);
        var yielder = new WaitForFixedUpdate();
        while (true)
        {
            float time = 0.0f;
            float duration = rand.NextRange(1.0f, 3.5f);
            float speed = rand.NextRange(1.5f, 4.0f);
            switch (rand.Next(0, 2))
            {
                case 0:
                    while (time <= duration)
                    {
                        time += Time.fixedDeltaTime;
                        float intensity = Mathf.PerlinNoise(Time.time * speed, 0.0f);
                        Light.ChangeIntensity(OrigIntensity * intensity);
                        yield return yielder;
                    }
                    break;

                case 1:
                    while (time <= duration)
                    {
                        float offDuration = rand.NextFloat() * 0.5f;
                        float onDuration = rand.NextFloat() * 0.5f;

                        Light.SetEnabled(false);
                        yield return new WaitForSeconds(offDuration);
                        time += offDuration;

                        Light.SetEnabled(true);
                        yield return new WaitForSeconds(onDuration);
                        time += onDuration;
                    }
                    break;
            }
        }
    }

    private void StopAnimation()
    {
        if (LightAnimationRoutine != null)
        {
            OwnerZone.StopCoroutine(LightAnimationRoutine);
        }
    }

    public void Revert()
    {
        Light.ChangeColor(OrigColor);
        Light.ChangeIntensity(OrigIntensity);
        Light.SetEnabled(OrigEnabled);
    }
}

public sealed class ZoneLightReplicator : MonoBehaviour, IStateReplicatorHolder<ZoneLightState>
{
    [HideFromIl2Cpp]
    public StateReplicator<ZoneLightState> Replicator { get; private set; }
    public LightWorker[] LightsInZone;
    public bool IsSetup { get; private set; } = false;

    public void Setup(LG_Zone zone)
    {
        /*Zone ID can be start with 0*/
        /*if (!StateReplicator<ZoneLightState>.TryCreate((uint)zone.ID + 1, new() { lightData = 0u }, LifeTimeType.Session, out var replicator, this))
        {
            Logger.Error("Failed to create ZoneLightReplicator!");
            return;
        }
        Replicator = replicator;*/
        Replicator = StateReplicator<ZoneLightState>.Create((uint)zone.ID + 1 /*Zone ID can be start with 0*/, new()
        {
            lightData = 0u
        }, LifeTimeType.Session, this);

        var workers = new List<LightWorker>();
        foreach (var nodes in zone.m_courseNodes)
        {
            foreach (var light in nodes.m_area.GetComponentsInChildren<LG_Light>(false))
            {
                workers.Add(new LightWorker()
                {
                    OwnerZone = zone,
                    Light = light,
                    InstanceID = light.GetInstanceID(),
                    PrefabIntensity = light.m_intensity,
                });
            }
        }
        LightsInZone = workers.ToArray();
        IsSetup = true;
    }

    public void Setup_UpdateLightSetting()
    {
        foreach (var worker in LightsInZone)
        {
            worker.OrigColor = worker.Light.m_color;
            worker.OrigIntensity = worker.Light.m_intensity;
            worker.OrigEnabled = worker.Light.gameObject.active;
            //LightAPI.LightsInLevelMap.TryAdd(worker.Light.GetInstanceID(), LightsInZone);
        }
    }

    void OnDestroy()
    {
        Replicator?.Unload();
    }

    [HideFromIl2Cpp]
    public void SetLightSetting(WEE_ZoneLightData data)
    {
        if (Replicator == null || Replicator.IsInvalid) return;

        var seed = data.Seed;
        if (seed == 0)
        {
            seed = EntryPoint.SessionRand.Next(int.MinValue, int.MaxValue);
        }

        Replicator.SetState(new ZoneLightState()
        {
            lightData = data.LightDataID,
            lightSeed = seed,
            duration = data.TransitionDuration
        });
    }

    public void RevertLightData()
    {
        if (Replicator == null || Replicator.IsInvalid) return;

        Replicator.SetState(new ZoneLightState() { lightData = 0 });
    }

    public void OnStateChange(ZoneLightState oldState, ZoneLightState state, bool isRecall)
    {
        if (state.lightData == 0u)
        {
            for (int i = 0; i < LightsInZone.Length; i++)
            {
                //if (LightsInZone[i].ExcludeFromZoneLightJob) continue;
                LightsInZone[i].Revert();
            }
        }
        else
        {
            var block = LightSettingsDataBlock.GetBlock(state.lightData);
            if (block == null || !block.internalEnabled)
            {
                Logger.Error("Failed to find enabled LightSettingsDataBlock!");
                return;
            }

            for (int i = 0; i < LightsInZone.Length; i++)
            {
                //if (LightsInZone[i].ExcludeFromZoneLightJob) continue;
                LightsInZone[i].ApplyLightSetting(block, isRecall ? 0.0f : state.duration, state.lightSeed, i);
            }
        }
    }
}
