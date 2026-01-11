using AmorLib.Networking.StateReplicators;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Replicators;

public sealed partial class ZoneLightReplicator : MonoBehaviour, IStateReplicatorHolder<ZoneLightState>
{
    [HideFromIl2Cpp]
    public event Action? OnLightsChanged;
    public Coroutine? ShareStatusCoroutine;

    public void ShareStatus()
    {
        OnLightsChanged?.Invoke();
    }

    public void StartShareStatus(float duration)
    {
        if (OnLightsChanged != null)
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
        float time = 0f;
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
        if (time < 2f)
        {
            return float.NaN;
        }
        else if (time < 10f)
        {
            return time / 2f;
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

        return Math.Min(interval, 30f);
    }
}
