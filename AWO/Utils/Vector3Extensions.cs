using FluffyUnderware.Curvy.Utils;
using UnityEngine;

namespace AWO.Utils;

public static class Vector3Extensions
{
    public static bool IsWithinSqrDistance(this Vector3 a, Vector3 b, float threshold, out float sqrDistance)
    {
        sqrDistance = (a - b).sqrMagnitude;
        return sqrDistance < threshold * threshold;
    }

    public static bool IsApproximatelySqrDistance(this Vector3 a, Vector3 b, float threshold, out float sqrDistance)
    {
        sqrDistance = (a - b).sqrMagnitude;
        return sqrDistance.Approximately(threshold * threshold);
    }
}
