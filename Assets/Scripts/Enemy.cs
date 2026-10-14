using System.Collections.Generic;
using UnityEngine;

/// <summary>Melee monster. Chases the human (or the ghost while taunted), attacks on contact then every attackInterval seconds.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> All = new List<Enemy>();

    public struct Stats
    {
        public float health, speed, damage, size;
        public int xp;
        public Color color;
    }

    const float SlowFactor = 0.6f;
    const float WeakenFactor = 0.7f;
    const float AttackRangePadding = 0.3f;
    const float KnockDuration = 0.15f;

    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsFrozen => Time.time < frozenUntil;
    public bool IsTaunted => Time.time < tauntUntil;
    public bool IsSlowed => Time.time < slowUntil;
    public bool IsWeakened => Time.time < weakUntil;

    /// <summary>True during the last moments of a freeze or taunt (drives the shake/flicker warning).</summary>
    public bool DebuffWearingOff
    {
        get
        {
            float now = Time.time;
            return (now < frozenUntil && frozenUntil - now <= warnTime) ||
                   (now < tauntUntil && tauntUntil - now <= warnTime);
        }
    }

    float moveSpeed, damage, attackInterval, nextAttackTime, warnTime = 0.8f;
    int xpValue;
    float frozenUntil, tauntUntil, slowUntil, weakUntil, flashUntil, knockUntil;
    float knockDur = KnockDuration;
    Vector2 knockVel;
    bool bodyFrozen, shaking;
    Vector3 baseScale = Vector3.one;
    Rigidbody2D rb;
    SpriteRenderer sr;
    Color baseColor;

    public static Enemy Spawn(Vector2 position, Stats s)
    {
        var gm = GameManager.Instance;
        var go = GameManager.Create(gm != null ? gm.enemyPrefab : null, "Enemy");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * s.size;

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = ProceduralAssets.AddSprite(go, ProceduralAssets.Blob, s.color, 0);

        var e = go.GetOrAdd<Enemy>();
        e.Init(s, sr);
        return e;
    }

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void Init(Stats s, SpriteRenderer renderer)
    {
        sr = renderer;
        baseColor = s.color;
        baseScale = transform.localScale;
        MaxHealth = Health = s.health;
        moveSpeed = s.speed;
        damage = s.damage;
        xpValue = s.xp;
        var tune = GameManager.Instance.tuning;
        attackInterval = tune.enemyAttackInterval;
        warnTime = tune.debuffWarningTime;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.mass = Mathf.Max(0.5f, s.size * s.size);
        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
        }
    }

    // ---------------- Queries ----------------
    public static Enemy Closest(Vector2 from, float maxRange)
    {
        Enemy best = null;
        float bestSq = maxRange * maxRange;
        for (int i = 0; i < All.Count; i++)
        {
            var e = All[i];
            if (e == null || e.IsDead) continue;
            float d = ((Vector2)e.transform.position - from).sqrMagnitude;
            if (d < bestSq) { bestSq = d; best = e; }
        }
        return best;
    }

    Transform CurrentTarget()
    {
        var gm = GameManager.Instance;
        if (gm == null) return null;
        if (IsTaunted && gm.GhostPlayer != null) return gm.GhostPlayer.transform;
        return gm.HumanPlayer != null ? gm.HumanPlayer.transform : null;
    }

    // ---------------- Ghost / player effects ----------------
    public void Freeze(float duration)
    {
        frozenUntil = Mathf.Max(frozenUntil, Time.time + duration);
        knockUntil = 0f; // cancel any push in progress; a frozen enemy doesn't move
    }

    public void Taunt(float duration) { tauntUntil = Mathf.Max(tauntUntil, Time.time + duration); }

    public void ApplyDebuff(float slowDuration, float weakenDuration)
    {
        slowUntil = Mathf.Max(slowUntil, Time.time + slowDuration);
        weakUntil = Mathf.Max(weakUntil, Time.time + weakenDuration);
    }

    /// <summary>Pushes the enemy back roughly 'distance' world units (ignored while frozen).</summary>
    public void Knockback(Vector2 direction, float distance)
    {
        if (IsDead || IsFrozen || distance <= 0.001f || direction.sqrMagnitude < 0.0001f) return;
        knockDur = Mathf.Clamp(0.1f + 0.05f * distance, 0.1f, 0.4f);
        // Velocity decays linearly to zero over knockDur, so total travel = v0 * knockDur / 2.
        knockVel = direction.normalized * (2f * distance / knockDur);
        knockUntil = Time.time + knockDur;
    }

    /// <param name="knockback">Optional knockback VELOCITY (used by melee). Ignored while frozen.</param>
    public void TakeDamage(float amount, Vector2 knockback = default)
    {
        if (IsDead) return;
        Health -= amount;
        flashUntil = Time.time + 0.08f;
        if (!IsFrozen && knockback.sqrMagnitude > 0.001f)
        {
            knockVel = knockback;
            knockDur = KnockDuration;
            knockUntil = Time.time + KnockDuration;
        }

        if (GameManager.Instance != null && GameManager.Instance.tuning.showDamageNumbers)
            Fx.Popup(transform.position + Vector3.up * 0.3f, Mathf.CeilToInt(amount).ToString(), Color.white);
        Sfx.Play(SfxId.Hit, 0.5f, 0.04f);

        if (Health <= 0f) Die();
    }

    void Die()
    {
        IsDead = true;
        All.Remove(this);
        Sfx.Play(SfxId.EnemyDie, 0.6f, 0.04f);
        Fx.Ring(transform.position, 0.2f, 0.8f, new Color(1f, 0.4f, 0.4f, 0.8f), 0.2f);
        if (Random.value < GameManager.Instance.tuning.xpDropChance)
            XpOrb.Spawn(transform.position, xpValue);
        Destroy(gameObject);
    }

    // ---------------- Behaviour ----------------
    void FixedUpdate()
    {
        if (IsDead) return;

        // Frozen enemies become KINEMATIC: the crowd behind them can't push them around.
        bool frozen = IsFrozen;
        if (frozen != bodyFrozen)
        {
            bodyFrozen = frozen;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = frozen ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        }
        if (frozen) return;

        Vector2 vel = Vector2.zero;
        if (Time.time < knockUntil)
        {
            vel = knockVel * ((knockUntil - Time.time) / knockDur);
        }
        else
        {
            var target = CurrentTarget();
            if (target != null)
            {
                Vector2 dir = ((Vector2)target.position - rb.position).normalized;
                vel = dir * moveSpeed * (IsSlowed ? SlowFactor : 1f);
            }
        }
        rb.linearVelocity = vel;
    }

    void Update()
    {
        if (IsDead) return;
        UpdateColor();
        UpdateShake();
        if (IsFrozen) return;

        var target = CurrentTarget();
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.position);
        float reach = baseScale.x * 0.5f + 0.4f + AttackRangePadding;
        // Attack the moment contact is made; the interval only gates the NEXT attack.
        // If the hit was absorbed (human invulnerable), keep retrying until it lands.
        if (dist <= reach && Time.time >= nextAttackTime && Attack(target))
            nextAttackTime = Time.time + attackInterval;
    }

    /// <summary>Vibrate (rotation wobble + scale jitter) while a freeze/taunt is about to expire.</summary>
    void UpdateShake()
    {
        if (DebuffWearingOff)
        {
            float k = Time.time * 70f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k) * 14f);
            transform.localScale = baseScale * (1f + 0.07f * Mathf.Sin(k * 1.37f));
            shaking = true;
        }
        else if (shaking)
        {
            transform.rotation = Quaternion.identity;
            transform.localScale = baseScale;
            shaking = false;
        }
    }

    /// <returns>true if the attack actually landed (or was a visual-only taunt attack).</returns>
    bool Attack(Transform target)
    {
        var gm = GameManager.Instance;
        if (gm.GhostPlayer != null && target == gm.GhostPlayer.transform)
        {
            // Taunted attack on the immortal ghost: visual only, no damage.
            Fx.Ring(target.position, 0.2f, 0.9f, new Color(1f, 0.6f, 0.2f, 0.7f), 0.2f);
            return true;
        }
        return gm.HumanPlayer != null && gm.HumanPlayer.TakeDamage(damage * (IsWeakened ? WeakenFactor : 1f));
    }

    void UpdateColor()
    {
        Color c = baseColor;
        if (Time.time < flashUntil) c = Color.white;
        else
        {
            if (IsFrozen) c = new Color(0.55f, 0.9f, 1f);
            else if (IsTaunted) c = new Color(1f, 0.65f, 0.2f);
            else
            {
                if (IsSlowed) c = Color.Lerp(c, new Color(0.3f, 0.4f, 1f), 0.5f);
                if (IsWeakened) c = Color.Lerp(c, new Color(0.7f, 0.3f, 0.9f), 0.5f);
            }
            if (DebuffWearingOff)
                c = Color.Lerp(c, Color.white, 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.time * 45f)));
        }
        sr.color = c;
    }
}