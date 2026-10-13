using UnityEngine;

/// <summary>Automatic attack at the closest enemy, plus Secondary Auto-Attack and Charged Attack support.</summary>
public class AutoShooter : MonoBehaviour
{
    public float baseDamage = 10f;
    public float fireInterval = 0.55f;
    public float range = 11f;
    public float projectileSpeed = 14f;
    public float projectileSize = 0.28f;
    public float knockback = 1.5f;
    public bool applyDebuff;
    public Color color = Color.yellow;
    public SfxId sound = SfxId.Shoot;
    /// <summary>Direction used by a charged shot when no enemy is in range.</summary>
    public Vector2 fallbackDirection = Vector2.right;

    float timer;

    float DamageMult => UpgradeSystem.Instance != null ? UpgradeSystem.Instance.DamageMultiplier : 1f;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.InputLocked) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        var target = Enemy.Closest(transform.position, range);
        if (target == null) return;
        timer = fireInterval;

        Vector2 dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        var up = UpgradeSystem.Instance;
        int hits = up != null ? up.PierceHits : 1;

        Projectile.Spawn(transform.position, dir, baseDamage * DamageMult, projectileSpeed, projectileSize, hits, applyDebuff, color, knockback);
        if (up != null && up.SecondaryUnlocked)
        {
            Projectile.Spawn(transform.position, -dir, baseDamage * DamageMult * up.SecondaryFraction, projectileSpeed,
                             projectileSize * 0.8f, hits, applyDebuff, Color.Lerp(color, Color.white, 0.4f), knockback * 0.5f);
        }
        Sfx.Play(sound, 0.6f, 0.05f);
    }

    public void FireCharged()
    {
        var up = UpgradeSystem.Instance;
        if (up == null || !up.ChargedUnlocked) return;

        var target = Enemy.Closest(transform.position, range * 1.5f);
        Vector2 dir = target != null
            ? ((Vector2)target.transform.position - (Vector2)transform.position).normalized
            : fallbackDirection.normalized;

        Projectile.Spawn(transform.position, dir, baseDamage * up.ChargeDamageMult * DamageMult, projectileSpeed * 1.2f,
                         projectileSize * up.ChargeSizeMult, Mathf.Max(up.PierceHits, 2), applyDebuff,
                         new Color(1f, 0.95f, 0.5f), knockback * 3f, 2.5f);
        Sfx.Play(SfxId.Charged, 0.8f, 0.05f);
    }
}

/// <summary>Ring around a character that grows while charging and pulses once fully charged.</summary>
public class ChargeVisual
{
    readonly SpriteRenderer ring;
    readonly Transform owner;
    readonly Color color;

    public ChargeVisual(Transform owner, Color color)
    {
        this.owner = owner;
        this.color = color;
        var go = new GameObject("ChargeRing");
        go.transform.SetParent(owner, false);
        ring = ProceduralAssets.AddSprite(go, ProceduralAssets.Ring, color, 7);
        ring.enabled = false;
    }

    public void Set(float progress, bool charged)
    {
        if (progress <= 0f) { ring.enabled = false; return; }
        ring.enabled = true;
        float size = charged ? 2.2f + 0.2f * Mathf.Sin(Time.unscaledTime * 20f) : Mathf.Lerp(3.2f, 2.2f, progress);
        float parentScale = Mathf.Max(0.01f, owner.localScale.x);
        ring.transform.localScale = Vector3.one * (size / parentScale);
        var c = charged ? new Color(1f, 0.95f, 0.4f, 1f) : new Color(color.r, color.g, color.b, 0.25f + 0.55f * progress);
        ring.color = c;
    }
}