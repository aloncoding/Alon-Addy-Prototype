using UnityEngine;

/// <summary>
/// Scales and fades a sprite over a short duration, then destroys it.
/// Optional: start after a delay, follow a transform (stay centred on it), or drift with a velocity.
/// </summary>
public class FxAnimator : MonoBehaviour
{
    SpriteRenderer sr;
    float t, duration, s0, s1, delay;
    Color color;
    Transform follow;
    Vector2 velocity;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    public void Setup(float startScale, float endScale, Color c, float dur,
                      float delay = 0f, Transform follow = null, Vector2 velocity = default)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        s0 = startScale; s1 = endScale; color = c; duration = Mathf.Max(0.01f, dur);
        this.delay = delay;
        this.follow = follow;
        this.velocity = velocity;
        transform.localScale = Vector3.one * s0;
        if (delay > 0f && sr != null) sr.enabled = false;
    }

    void Update()
    {
        if (follow != null) transform.position = new Vector3(follow.position.x, follow.position.y, transform.position.z);

        if (delay > 0f)
        {
            delay -= Time.deltaTime;
            if (delay > 0f) return;
            if (sr != null) sr.enabled = true;
        }

        t += Time.deltaTime;
        float k = t / duration;
        if (k >= 1f) { Destroy(gameObject); return; }
        transform.localScale = Vector3.one * Mathf.Lerp(s0, s1, k);
        if (follow == null && velocity != Vector2.zero) transform.position += (Vector3)(velocity * Time.deltaTime);
        var c = color;
        c.a = color.a * (1f - k * k);
        if (sr != null) sr.color = c;
    }
}