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

    /// <summary>Green healing pulse centred on (and following) the target: soft glow, three staggered rings, rising motes.</summary>
    public static void HealPulse(Transform target)
    {
        if (target == null) return;
        var green = new Color(0.35f, 1f, 0.5f, 1f);

        // Soft glow disc, drawn just behind the characters.
        var glow = GameManager.Create(null, "FxHealGlow");
        glow.transform.position = target.position;
        var glowSr = ProceduralAssets.AddSprite(glow, ProceduralAssets.Circle, new Color(green.r, green.g, green.b, 0.45f), 4);
        glow.AddComponent<FxAnimator>().Setup(1f, 7f, glowSr.color, 0.7f, 0f, target);

        // Three rings expanding one after another.
        for (int i = 0; i < 3; i++)
        {
            var ring = GameManager.Create(null, "FxHealRing");
            ring.transform.position = target.position;
            var ringSr = ProceduralAssets.AddSprite(ring, ProceduralAssets.Ring, new Color(green.r, green.g, green.b, 0.9f), 8);
            ring.AddComponent<FxAnimator>().Setup(0.8f, 6.5f, ringSr.color, 0.7f, i * 0.15f, target);
        }

        // Rising motes.
        for (int i = 0; i < 10; i++)
        {
            var mote = GameManager.Create(null, "FxHealMote");
            Vector2 offset = Random.insideUnitCircle * 1.3f;
            mote.transform.position = (Vector2)target.position + offset;
            var moteSr = ProceduralAssets.AddSprite(mote, ProceduralAssets.Circle, new Color(0.7f, 1f, 0.75f, 0.9f), 9);
            mote.AddComponent<FxAnimator>().Setup(0.22f, 0.08f, moteSr.color, Random.Range(0.6f, 1f), 0f, null, new Vector2(0f, Random.Range(1.2f, 2.2f)));
        }
    }

    public static void Popup(Vector3 worldPos, string text, Color color)
    {
        if (HUD.Instance != null) HUD.Instance.AddPopup(worldPos, text, color);
    }
}