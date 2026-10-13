using UnityEngine;

/// <summary>Scales and fades a sprite over a short duration, then destroys it.</summary>
public class FxAnimator : MonoBehaviour
{
    SpriteRenderer sr;
    float t, duration, s0, s1;
    Color color;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    public void Setup(float startScale, float endScale, Color c, float dur)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        s0 = startScale; s1 = endScale; color = c; duration = Mathf.Max(0.01f, dur);
        transform.localScale = Vector3.one * s0;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = t / duration;
        if (k >= 1f) { Destroy(gameObject); return; }
        transform.localScale = Vector3.one * Mathf.Lerp(s0, s1, k);
        var c = color;
        c.a = color.a * (1f - k * k);
        if (sr != null) sr.color = c;
    }
}