using AWO.Modules.WEE.Replicators;
using GTFO.API;
using LevelGeneration;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace AWO.API;

public static class LightAPI
{
    /*internal readonly static ConcurrentDictionary<int, LightWorker[]> LightsInLevelMap = new();

    static LightAPI()
    {
        LevelAPI.OnLevelCleanup += OnLevelCleanup;
    }

    private static void OnLevelCleanup()
    {
        LightsInLevelMap.Clear();
    }

    public static void SetLightJobExclusivity(LG_Light light, bool exclude)
    {
        if (!TryGetLightWorkerFromLight(light, out var lightWorker)) return;

        lightWorker.ExcludeFromZoneLightJob = exclude;

        if (!exclude)
        {
            if (lightWorker.LightAnimationRoutine != null)
            {
                lightWorker.OwnerZone.StopCoroutine(lightWorker.LightAnimationRoutine);
            }
            if (lightWorker.LightTransitionRoutine != null)
            {
                lightWorker.OwnerZone.StopCoroutine(lightWorker.LightTransitionRoutine);
            }
        }
    }

    public static bool TryGetCurrentLightData(LG_Light light, out float intensity, out Color color)
    {
        if (TryGetLightWorkerFromLight(light, out var lightWorker))
        {
            intensity = lightWorker.Light.GetIntensity();
            color = lightWorker.Light.m_color;
            return true;
        }

        intensity = -1.0f;
        color = Color.black;
        return false;
    }

    internal static bool TryGetLightWorkerFromLight(LG_Light light, [NotNullWhen(true)] out LightWorker? lightWorker)
    {
        lightWorker = null;
        int id = light.GetInstanceID();
        if (LightsInLevelMap.TryGetValue(id, out var workers))
        { 
            lightWorker = workers.FirstOrDefault(lw => lw.InstanceID == id);
        }
        return lightWorker != null;
    }*/
}
