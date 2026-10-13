using UnityEngine;

/// <summary>Static helpers for quick visual feedback (expanding rings, floating numbers).</summary>
public static class Fx
{
    public static void Ring(Vector2 pos, float startRadius, float endRadius, Color color, float duration, int order = 8)
    {
        var gm = GameManager.Instance;
        var go = GameManager.Create(gm != null ? gm.fxRingPrefab : null, "FxRing");
        go.transform.position = pos;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = ProceduralAssets.AddSprite(go, ProceduralAssets.Ring, color, order);
        sr.color = color;
        go.GetOrAdd<FxAnimator>().Setup(startRadius * 2f, endRadius * 2f, color, duration);
    }

    public static void Popup(Vector3 worldPos, string text, Color color)
    {
        if (HUD.Instance != null) HUD.Instance.AddPopup(worldPos, text, color);
    }
}