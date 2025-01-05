using UnityEngine;

namespace AWO.Utils;

public static class GameObjectExtensions
{
    public static bool TryAndGetComponent<T>(this GameObject go, out T component)
    {
        component = go.GetComponent<T>();
        return component != null;
    }

    public static T AddOrGetComponent<T>(this GameObject go) where T : Component
    {
        if (!TryAndGetComponent(go, out T comp))
        {
            comp = go.AddComponent<T>();
        }
        return comp;
    }
}
