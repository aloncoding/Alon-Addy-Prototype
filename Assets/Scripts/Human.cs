using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player One. WASD move, Space melee (hold 0.4s + release = charged shot once unlocked), Shift dash, auto gunfire.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Human : MonoBehaviour
{
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public bool IsDead { get; private set; }
    public float ChargeProgress { get; private set; }
    public bool IsCharged { get; private set; }
    public readonly AbilityCooldown Melee = new AbilityCooldown();
    public readonly AbilityCooldown Dash = new AbilityCooldown();

    GameTuning tune;
    Rigidbody2D rb;
    SpriteRenderer sr;
    AutoShooter shooter;
    ChargeVisual chargeVisual;
    Color baseColor = new Color(0.45f, 0.8f, 1f);
    Vector2 moveInput;
    Vector2 facing = Vector2.right;
    bool dashing;
    Vector2 dashDir;
    float dashEnd, invulnUntil, flashUntil;
    bool charging;
    float chargeTimer;

    void Awake()
    {
        var gm = GameManager.Instance;
        tune = gm.tuning;
        MaxHealth = Health = tune.humanMaxHealth;

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            // No prefab: build placeholder visuals/physics in code.
            rb.mass = 3f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            transform.localScale = Vector3.one * 0.9f;
            sr = ProceduralAssets.AddSprite(gameObject, ProceduralAssets.Blob, baseColor, 5);
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
        }
        else baseColor = sr.color; // prefab colour is the base colour

        shooter = gameObject.GetOrAdd<AutoShooter>();
        shooter.baseDamage = tune.gunDamage;
        shooter.fireInterval = tune.gunFireInterval;
        shooter.color = new Color(1f, 0.9f, 0.3f);
        shooter.sound = SfxId.Shoot;
        chargeVisual = new ChargeVisual(transform, new Color(1f, 0.9f, 0.3f));
    }

    void Update()
    {
        UpdateColor();
        if (IsDead) return;
        var gm = GameManager.Instance;
        if (gm.InputLocked) { moveInput = Vector2.zero; CancelCharge(); return; }

        var kb = Keyboard.current;
        if (kb == null) return;

        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float y = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        moveInput = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        if (moveInput.sqrMagnitude > 0.01f) facing = moveInput.normalized;
        shooter.fallbackDirection = facing;

        if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame) TryDash();

        // Melee fires instantly on press (always responsive); holding the same key also charges a shot.
        if (kb.spaceKey.wasPressedThisFrame)
        {
            TryMelee();
            charging = true;
            chargeTimer = 0f;
        }
        UpdateCharge(kb.spaceKey.isPressed);

        if (dashing && Time.time >= dashEnd) dashing = false;
    }

    void FixedUpdate()
    {
        if (IsDead) { rb.linearVelocity = Vector2.zero; return; }
        rb.linearVelocity = dashing ? dashDir * tune.dashSpeed : moveInput * tune.humanMoveSpeed;
    }

    // ---------------- Abilities ----------------
    void TryMelee()
    {
        if (!Melee.IsReady) return;
        bool free = Melee.Start(tune.meleeCooldown);
        float dmg = tune.meleeDamage * (UpgradeSystem.Instance != null ? UpgradeSystem.Instance.DamageMultiplier : 1f);
        Vector2 pos = transform.position;

        var snapshot = new List<Enemy>(Enemy.All);
        foreach (var e in snapshot)
        {
            if (e == null || e.IsDead) continue;
            Vector2 to = (Vector2)e.transform.position - pos;
            if (to.magnitude <= tune.meleeRadius + e.transform.localScale.x * 0.5f)
                e.TakeDamage(dmg, to.normalized * tune.meleeKnockback);
        }
        Fx.Ring(pos, 0.4f, tune.meleeRadius, new Color(1f, 1f, 1f, 0.9f), 0.18f);
        Sfx.Play(SfxId.Melee, 0.8f, 0.05f);
        if (free) { Fx.Popup(transform.position + Vector3.up * 1.2f, "FREE!", new Color(0.8f, 0.5f, 1f)); Sfx.Play(SfxId.Free); }
    }

    void TryDash()
    {
        if (!Dash.IsReady || dashing) return;
        bool free = Dash.Start(tune.dashCooldown);
        dashDir = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : facing;
        dashing = true;
        dashEnd = Time.time + tune.dashDuration;
        invulnUntil = Mathf.Max(invulnUntil, Time.time + tune.dashInvulnerability);
        Fx.Ring(transform.position, 0.3f, 1.4f, new Color(0.6f, 0.9f, 1f, 0.8f), 0.25f);
        Sfx.Play(SfxId.Dash, 0.8f, 0.05f);
        if (free) { Fx.Popup(transform.position + Vector3.up * 1.2f, "FREE!", new Color(0.8f, 0.5f, 1f)); Sfx.Play(SfxId.Free); }
    }

    void UpdateCharge(bool held)
    {
        if (!charging) return;
        var up = UpgradeSystem.Instance;
        if (held)
        {
            if (up != null && up.ChargedUnlocked)
            {
                chargeTimer += Time.deltaTime;
                if (!IsCharged && chargeTimer >= UpgradeSystem.ChargeTime)
                {
                    IsCharged = true;
                    Sfx.Play(SfxId.Charged);                                          // SFX when fully charged
                    Fx.Ring(transform.position, 0.3f, 1.8f, new Color(1f, 0.95f, 0.4f), 0.3f); // VFX when fully charged
                }
            }
        }
        else
        {
            if (IsCharged) shooter.FireCharged();
            CancelCharge();
            return;
        }
        ChargeProgress = (up != null && up.ChargedUnlocked) ? Mathf.Clamp01(chargeTimer / UpgradeSystem.ChargeTime) : 0f;
        chargeVisual.Set(ChargeProgress, IsCharged);
    }

    void CancelCharge()
    {
        charging = false;
        chargeTimer = 0f;
        IsCharged = false;
        ChargeProgress = 0f;
        if (chargeVisual != null) chargeVisual.Set(0f, false);
    }

    // ---------------- Health ----------------
    public void TakeDamage(float amount)
    {
        if (IsDead || Time.time < invulnUntil) return;
        Health = Mathf.Max(0f, Health - amount);
        invulnUntil = Time.time + tune.humanHitInvulnerability;
        flashUntil = Time.time + 0.15f;
        Fx.Popup(transform.position + Vector3.up * 0.8f, Mathf.CeilToInt(amount).ToString(), new Color(1f, 0.3f, 0.3f));
        Sfx.Play(SfxId.Hurt);
        if (Health <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        float before = Health;
        Health = Mathf.Min(MaxHealth, Health + amount);
        Fx.Popup(transform.position + Vector3.up * 0.8f, "+" + Mathf.RoundToInt(Health - before), new Color(0.4f, 1f, 0.5f));
        Fx.Ring(transform.position, 0.3f, 1.6f, new Color(0.4f, 1f, 0.5f, 0.8f), 0.4f);
        Sfx.Play(SfxId.Heal);
    }

    void Die()
    {
        IsDead = true;
        CancelCharge();
        rb.linearVelocity = Vector2.zero;
        Sfx.Play(SfxId.Death);
        GameManager.Instance.OnHumanDied();
    }

    void UpdateColor()
    {
        Color c = baseColor;
        var up = UpgradeSystem.Instance;
        if (up != null && up.BoostActive) c = Color.Lerp(c, new Color(1f, 0.85f, 0.2f), 0.5f);
        if (Time.time < flashUntil) c = Color.Lerp(c, Color.red, 0.6f);
        else if (Time.time < invulnUntil) c.a = 0.55f + 0.2f * Mathf.Sin(Time.time * 40f);
        if (IsDead) c = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        sr.color = c;
    }
}