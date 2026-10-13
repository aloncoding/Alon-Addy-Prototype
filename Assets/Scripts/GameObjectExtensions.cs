using UnityEngine;

public static class GameObjectExtensions
{
    /// <summary>Safe GetOrAdd (avoids the '??' operator, which is unreliable on UnityEngine.Object).</summary>
    public static T GetOrAdd<T>(this GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }
}