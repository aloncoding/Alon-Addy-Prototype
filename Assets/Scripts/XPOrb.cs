using System.Collections.Generic;
using UnityEngine;

/// <summary>Experience pickup. Either player can collect it; it magnets toward the nearest one.</summary>
public class XpOrb : MonoBehaviour
{
    public static readonly List<XpOrb> All = new List<XpOrb>();

    const float MagnetRange = 3.5f;
    const float CollectRange = 0.55f;

    int value;
    float life = 90f;
    float speed;
    float baseScale;
    bool collected;

    public static void Spawn(Vector2 pos, int value)
    {
        var gm = GameManager.Instance;
        var go = GameManager.Create(gm != null ? gm.xpOrbPrefab : null, "XpOrb");
        go.transform.position = pos + Random.insideUnitCircle * 0.3f;
        float scale = 0.2f + 0.05f * Mathf.Sqrt(value);
        go.transform.localScale = Vector3.one * scale;

        Color color = Color.Lerp(new Color(0.4f, 1f, 0.5f), new Color(1f, 0.85f, 0.2f), Mathf.InverseLerp(1f, 8f, value));
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = ProceduralAssets.AddSprite(go, ProceduralAssets.Circle, color, 1);
        sr.color = color;

        var orb = go.GetOrAdd<XpOrb>();
        orb.value = value;
        orb.baseScale = scale;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.GameOver || collected) return;

        life -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }
        transform.localScale = Vector3.one * baseScale * (1f + 0.12f * Mathf.Sin(Time.time * 6f));

        Vector2 pos = transform.position;
        Vector2 targetPos = pos;
        float best = float.MaxValue;
        if (gm.HumanPlayer != null && !gm.HumanPlayer.IsDead)
        {
            Vector2 h = gm.HumanPlayer.transform.position;
            float d = Vector2.Distance(pos, h);
            if (d < best) { best = d; targetPos = h; }
        }
        if (gm.GhostPlayer != null)
        {
            Vector2 g = gm.GhostPlayer.transform.position;
            float d = Vector2.Distance(pos, g);
            if (d < best) { best = d; targetPos = g; }
        }

        if (best <= CollectRange)
        {
            collected = true;
            Sfx.Play(SfxId.Pickup, 1f, 0.03f);
            Destroy(gameObject);
            gm.AddXp(value);
            return;
        }
        if (best <= MagnetRange)
        {
            speed += 30f * Time.deltaTime;
            transform.position = Vector2.MoveTowards(pos, targetPos, (6f + speed) * Time.deltaTime);
        }
        else speed = 0f;
    }
}