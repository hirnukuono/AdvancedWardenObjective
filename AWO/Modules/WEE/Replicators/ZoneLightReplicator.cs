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
    public readonly LG_Zone OwnerZone;
    public readonly LG_Light Light;
    public readonly int InstanceID;
    public readonly float PrefabIntensity;
    public LG_LightAnimator? Animator;
    public Color OrigColor; 
    public bool OrigEnabled;    
    public float OrigIntensity;    
    public Coroutine? LightAnimationCoroutine;
    public Coroutine? LightTransitionCoroutine;

    public LightWorker(LG_Zone a, LG_Light b, int c, float d)
    {
        OwnerZone = a;
        Light = b;
        InstanceID = c;
        PrefabIntensity = d;
    }

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
            ToggleFlicker(false);

            if (!rand.MeetProbability(setting.Chance))
            {
                LightTransitionCoroutine = OwnerZone.StartCoroutine(LightTransition(new()
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
                LightTransitionCoroutine = OwnerZone.StartCoroutine(LightTransition(new()
                {
                    startColor = Light.m_color,
                    endColor = setting.Color,
                    startIntensity = Light.m_intensity,
                    endIntensity = PrefabIntensity * setting.IntensityMul,
                    endMode = rand.MeetProbability(setting.ChanceBroken) ? LightTransitionData.Mode.Flickering : LightTransitionData.Mode.Enabled,
                    endModeSeed = rand.Next()
                }, duration));
            }
        }
    }

    private IEnumerator LightTransition(LightTransitionData data, float duration)
    {
        float time = 0.0f; 
        float progress;
        var yielder = new WaitForFixedUpdate();

        while (time <= duration)
        {
            time += Time.fixedDeltaTime;
            progress = time / duration;
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
                LightAnimationCoroutine = OwnerZone.StartCoroutine(LightAnimation(data.endModeSeed));
                break;
        }
    }

    private IEnumerator LightAnimation(int seed)
    {
        float time, duration, delay, ramp;
        var rand = new System.Random(seed);
        var yielder = new WaitForFixedUpdate();

        while (true)
        {
            time = 0.0f;
            duration = rand.NextRange(0.1f, 2.5f);
            delay = rand.NextRange(0.2f, 1.0f);

            while (time <= duration)
            {
                time += Time.fixedDeltaTime;
                ramp = time / duration;
                Light.ChangeIntensity(OrigIntensity * ramp * ramp);
                yield return yielder;
            }

            Light.ChangeIntensity(0.0f);
            yield return new WaitForSeconds(delay);
        }
    }

    public void ToggleFlicker(bool enabled)
    {
        if (Animator != null)
        {
            if (enabled)
            {
                Animator.ResetRamp(Light.GetC_Light());
            }
            else
            {
                Animator.m_inRamp = false;
                Animator.m_startTime = float.MaxValue;
                Animator.m_absTime = 1.0f;
                Animator.FeedLight(Light.GetC_Light());
            }
        }
    }

    public void StopAnimation()
    {
        if (LightAnimationCoroutine != null)
        {
            OwnerZone.StopCoroutine(LightAnimationCoroutine);
        }
    }

    public void Revert()
    {
        StopAnimation();        
        Light.ChangeColor(OrigColor);
        Light.ChangeIntensity(OrigIntensity);
        Light.SetEnabled(OrigEnabled);
        ToggleFlicker(true);
    }
}

public sealed class ZoneLightReplicator : MonoBehaviour, IStateReplicatorHolder<ZoneLightState>
{
    [HideFromIl2Cpp]
    public StateReplicator<ZoneLightState>? Replicator { get; private set; }
    public LG_Zone? Zone;
    public LightWorker[] LightsInZone = Array.Empty<LightWorker>();
    [HideFromIl2Cpp]
    public event Action? OnSharedStatus;
    public Coroutine? ShareStatusCoroutine;
    public bool IsSetup { get; private set; } = false;

    public void Setup(LG_Zone zone)
    {
        Replicator = StateReplicator<ZoneLightState>.Create((uint)zone.ID + 1 /*Zone ID can be start with 0*/, new()
        {
            lightData = 0u
        }, LifeTimeType.Session, this);

        List<LightWorker> workers = new();
        foreach (var nodes in zone.m_courseNodes)
        {
            foreach (var light in nodes.m_area.GetComponentsInChildren<LG_Light>(false))
            {
                workers.Add(new LightWorker(zone, light, light.GetInstanceID(), light.m_intensity));
            }
        }

        LightsInZone = workers.ToArray();
        Zone = zone;
        IsSetup = true;
    }

    public void PostSetup()
    {
        foreach (var worker in LightsInZone)
        {
            worker.Animator = worker.Light.gameObject.GetComponent<LG_LightAnimator>();
            worker.OrigColor = worker.Light.m_color;
            worker.OrigIntensity = worker.Light.m_intensity;
            worker.OrigEnabled = worker.Light.gameObject.active;            
        }
    }

    public void OnDestroy()
    {
        Replicator?.Unload();
    }

    [HideFromIl2Cpp]
    public void SetLightSetting(WEE_ZoneLightData data)
    {
        Replicator?.SetState(new ZoneLightState()
        {
            lightData = data.LightDataID,
            lightSeed = data.Seed,
            duration = data.TransitionDuration
        });        
    }

    public void RevertLightData()
    {
        Replicator?.SetState(new ZoneLightState() { lightData = 0u });        
    }

    public void OnStateChange(ZoneLightState oldState, ZoneLightState state, bool isRecall)
    {
        if (state.lightData == 0u) // revert light settings
        {
            for (int i = 0; i < LightsInZone.Length; i++)
            {
                LightsInZone[i].Revert();
            }
            StopShareStatus();
            ShareStatus();
        }
        else // set new light settings
        {
            var block = LightSettingsDataBlock.GetBlock(state.lightData);
            if (block == null || !block.internalEnabled)
            {
                Logger.Error("Failed to find enabled LightSettingsDataBlock!");
                return;
            }

            for (int i = 0; i < LightsInZone.Length; i++)
            {
                LightsInZone[i].ApplyLightSetting(block, isRecall ? 0.0f : state.duration, state.lightSeed, i);
            }
            StartShareStatus(state.duration);
        }
    }

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
        var yielder = new WaitForFixedUpdate();

        while (time <= duration)
        {
            time += Time.fixedDeltaTime;
            if (shouldSync && nextInvoke <= time)
            {
                ShareStatus();
                nextInvoke += interval;
            }
            yield return yielder;
        }

        ShareStatus();
    }

    private static float GetInvocationInterval(float time) // a very overcomplicated way to get faster zone light change sync intervals
    {
        if (time < 10.0f)
        {
            return float.NaN;
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
