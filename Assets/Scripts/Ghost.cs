using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player Two. Mouse moves the ghost, Left Click = Scare (freeze), Right Click = Taunt, auto debuff projectile.
/// With Charged Attack unlocked, holding Left Click 0.4s then releasing fires a charged shot (Scare still fires instantly on press).
/// The ghost is immortal and has no collider.
/// </summary>
public class Ghost : MonoBehaviour
{
    public float ChargeProgress { get; private set; }
    public bool IsCharged { get; private set; }
    public readonly AbilityCooldown Scare = new AbilityCooldown();
    public readonly AbilityCooldown Taunt = new AbilityCooldown();

    GameTuning tune;
    SpriteRenderer sr;
    AutoShooter shooter;
    ChargeVisual chargeVisual;
    Color baseColor = new Color(0.8f, 0.9f, 1f, 0.9f);
    Vector2 lastMoveDir = Vector2.right;
    bool charging;
    float chargeTimer;

    void Awake()
    {
        var gm = GameManager.Instance;
        tune = gm.tuning;
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = ProceduralAssets.AddSprite(gameObject, ProceduralAssets.Ghost, baseColor, 6);
            transform.localScale = Vector3.one * 1.1f;
        }
        else baseColor = sr.color;

        shooter = gameObject.GetOrAdd<AutoShooter>();
        shooter.baseDamage = tune.ghostDamage;
        shooter.fireInterval = tune.ghostFireInterval;
        shooter.applyDebuff = true;
        shooter.color = new Color(0.7f, 0.5f, 1f);
        shooter.sound = SfxId.GhostShoot;
        shooter.knockback = 0.5f;
        chargeVisual = new ChargeVisual(transform, new Color(0.7f, 0.5f, 1f));
    }

    void Update()
    {
        UpdateColor();
        var gm = GameManager.Instance;
        if (gm.InputLocked) { CancelCharge(); return; }
        var mouse = Mouse.current;
        if (mouse == null) return;

        MoveToCursor(mouse, gm);
        shooter.fallbackDirection = lastMoveDir;

        if (mouse.rightButton.wasPressedThisFrame) TryTaunt();
        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryScare();
            charging = true;
            chargeTimer = 0f;
        }
        UpdateCharge(mouse.leftButton.isPressed);
    }

    void MoveToCursor(Mouse mouse, GameManager gm)
    {
        var cam = DynamicCamera.Instance;
        if (cam == null) return;
        Vector2 pos = transform.position;
        Vector2 target = cam.ScreenToWorldClamped(mouse.position.ReadValue());

        Vector2 step = (target - pos) * (1f - Mathf.Exp(-14f * Time.deltaTime));
        float maxStep = tune.ghostMaxSpeed * Time.deltaTime;
        if (step.magnitude > maxStep) step = step.normalized * maxStep;
        if (step.sqrMagnitude > 0.0001f) lastMoveDir = step.normalized;
        pos += step;

        // Tether: the ghost can never be further from the human than the camera's max zoom-out can show.
        if (gm.HumanPlayer != null) pos = cam.ClampToTether(gm.HumanPlayer.transform.position, pos);
        transform.position = pos;
    }

    void TryScare()
    {
        if (!Scare.IsReady) return;
        bool free = Scare.Start(tune.scareCooldown);
        Vector2 pos = transform.position;
        foreach (var e in new List<Enemy>(Enemy.All))
        {
            if (e == null || e.IsDead) continue;
            if (Vector2.Distance(pos, e.transform.position) <= tune.scareRadius) e.Freeze(tune.scareDuration);
        }
        Fx.Ring(pos, 0.5f, tune.scareRadius, new Color(0.5f, 0.9f, 1f, 0.9f), 0.5f);
        Sfx.Play(SfxId.Scare);
        if (free) { Fx.Popup(transform.position + Vector3.up * 1.2f, "FREE!", new Color(0.8f, 0.5f, 1f)); Sfx.Play(SfxId.Free); }
    }

    void TryTaunt()
    {
        if (!Taunt.IsReady) return;
        bool free = Taunt.Start(tune.tauntCooldown);
        Vector2 pos = transform.position;
        foreach (var e in new List<Enemy>(Enemy.All))
        {
            if (e == null || e.IsDead) continue;
            if (Vector2.Distance(pos, e.transform.position) <= tune.tauntRadius) e.Taunt(tune.tauntDuration);
        }
        Fx.Ring(pos, 0.5f, tune.tauntRadius, new Color(1f, 0.6f, 0.2f, 0.9f), 0.5f);
        Sfx.Play(SfxId.Taunt);
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
                    Sfx.Play(SfxId.Charged);
                    Fx.Ring(transform.position, 0.3f, 1.8f, new Color(0.8f, 0.6f, 1f), 0.3f);
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

    void UpdateColor()
    {
        Color c = baseColor;
        var up = UpgradeSystem.Instance;
        if (up != null && up.BoostActive) c = Color.Lerp(c, new Color(1f, 0.85f, 0.2f, 0.95f), 0.5f);
        c.a *= 0.85f + 0.15f * Mathf.Sin(Time.time * 3f);
        sr.color = c;
    }
}