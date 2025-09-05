using UnityEngine;

namespace AWO.Utils;

public static class Vector3Extensions
{
    public static bool IsWithinSqrDistance(this Vector3 a, Vector3 b, float sqrThreshold, out float sqrDistance)
    {
        sqrDistance = (a - b).sqrMagnitude;
        return sqrDistance <= sqrThreshold;
    }
}
