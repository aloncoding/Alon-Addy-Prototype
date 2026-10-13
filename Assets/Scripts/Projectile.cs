using UnityEngine;

/// <summary>Trigger projectile used by both players. Ghost projectiles apply slow + weaken.</summary>
public class Projectile : MonoBehaviour
{
    float damage, life, knockback;
    int hitsLeft;
    bool debuff;
    Vector2 direction;

    public static Projectile Spawn(Vector2 pos, Vector2 dir, float damage, float speed, float size,
                                   int hits, bool debuff, Color color, float knockback, float life = 2f)
    {
        var gm = GameManager.Instance;
        var go = GameManager.Create(gm != null ? gm.projectilePrefab : null, debuff ? "GhostShot" : "Bullet");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * size;

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = ProceduralAssets.AddSprite(go, ProceduralAssets.Circle, color, 4);
        sr.color = color;

        var rb = go.GetOrAdd<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = dir * speed;

        var col = go.GetComponent<Collider2D>();
        if (col == null)
        {
            var cc = go.AddComponent<CircleCollider2D>();
            cc.radius = 0.5f;
            col = cc;
        }
        col.isTrigger = true;

        var p = go.GetOrAdd<Projectile>();
        p.damage = damage;
        p.hitsLeft = Mathf.Max(1, hits);
        p.debuff = debuff;
        p.knockback = knockback;
        p.life = life;
        p.direction = dir;
        return p;
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hitsLeft <= 0) return;
        var e = other.GetComponentInParent<Enemy>();
        if (e == null || e.IsDead) return;

        if (debuff)
        {
            var t = GameManager.Instance.tuning;
            e.ApplyDebuff(t.ghostSlowDuration, t.ghostWeakenDuration);
        }
        e.TakeDamage(damage, direction * knockback);
        hitsLeft--;
        if (hitsLeft <= 0) Destroy(gameObject);
    }
}