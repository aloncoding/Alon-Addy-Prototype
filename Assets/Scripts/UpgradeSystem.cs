using System.Collections.Generic;
using UnityEngine;

public enum UpgradeType
{
    PiercingAttacks = 0,
    BurstHealing = 1,
    AttackBoost = 2,
    ChargedAttack = 3,
    CooldownRefund = 4,
    SecondaryAttack = 5
}

/// <summary>
/// Owns upgrade levels, the six-entry random bag, and the automatic power-ups (Burst Healing, Attack Boost).
/// All effects are shared: both characters read their numbers from here.
/// </summary>
public class UpgradeSystem : MonoBehaviour
{
    public static UpgradeSystem Instance { get; private set; }
    public const int TypeCount = 6;

    public readonly int[] Levels = new int[TypeCount];
    readonly List<UpgradeType> bag = new List<UpgradeType>();
    readonly AbilityCooldown boostCd = new AbilityCooldown();
    float boostActiveUntil;
    bool healStarted;
    float healTimer;   // seconds until Burst Healing is allowed to fire again

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    // ---------------- Per-level formulas (numbers come from GameManager > Tuning > Power-Ups) ----------------
    static readonly GameTuning FallbackTuning = new GameTuning();
    static GameTuning T => GameManager.Instance != null ? GameManager.Instance.tuning : FallbackTuning;
    static int Steps(int lv) => Mathf.Max(0, lv - 1);

    public static float ChargeTime => Mathf.Max(0.05f, T.chargeTime);
    public static int PierceHitsAt(int lv) => lv <= 0 ? 1 : Mathf.Max(1, T.pierceHitsAtLevel1 + T.pierceExtraHitsPerLevel * Steps(lv));
    public static float HealPercentAt(int lv) => Mathf.Clamp01(T.healBasePercent + T.healPercentPerLevel * Steps(lv));
    public static float HealCooldownAt(int lv) => Mathf.Max(1f, Mathf.Max(T.healMinCooldown, T.healBaseCooldown - T.healCooldownReductionPerLevel * Steps(lv)));
    public static float BoostBonusAt(int lv) => Mathf.Max(0f, T.boostBaseBonus + T.boostBonusPerLevel * Steps(lv));
    public static float BoostDurationAt(int lv) => Mathf.Max(0.1f, T.boostBaseDuration + T.boostDurationPerLevel * Steps(lv));
    public static float BoostCooldownAt(int lv) => Mathf.Max(1f, Mathf.Max(T.boostMinCooldown, T.boostBaseCooldown - T.boostCooldownReductionPerLevel * Steps(lv)));
    public static float ChargeDamageMultAt(int lv) => Mathf.Max(0f, T.chargeBaseDamageMult + T.chargeDamageMultPerLevel * Steps(lv));
    public static float ChargeSizeMultAt(int lv) => Mathf.Max(0.1f, T.chargeBaseSizeMult + T.chargeSizeMultPerLevel * Steps(lv));
    public static float RefundChanceAt(int lv) => lv <= 0 ? 0f : Mathf.Clamp(T.refundBaseChance + T.refundChancePerLevel * Steps(lv), 0f, T.refundMaxChance);
    public static float SecondaryFractionAt(int lv) => lv <= 0 ? 0f : Mathf.Clamp(T.secondaryBaseFraction + T.secondaryFractionPerLevel * Steps(lv), 0f, T.secondaryMaxFraction);

    // ---------------- Live values used by gameplay ----------------
    public int Level(UpgradeType t) => Levels[(int)t];
    public int PierceHits => PierceHitsAt(Level(UpgradeType.PiercingAttacks));
    public bool ChargedUnlocked => Level(UpgradeType.ChargedAttack) > 0;
    public float ChargeDamageMult => ChargeDamageMultAt(Level(UpgradeType.ChargedAttack));
    public float ChargeSizeMult => ChargeSizeMultAt(Level(UpgradeType.ChargedAttack));
    public float RefundChance => RefundChanceAt(Level(UpgradeType.CooldownRefund));
    public bool SecondaryUnlocked => Level(UpgradeType.SecondaryAttack) > 0;
    public float SecondaryFraction => SecondaryFractionAt(Level(UpgradeType.SecondaryAttack));
    public bool BoostActive => Level(UpgradeType.AttackBoost) > 0 && Time.time < boostActiveUntil;
    public float BoostTimeLeft => Mathf.Max(0f, boostActiveUntil - Time.time);
    public float DamageMultiplier => BoostActive ? 1f + BoostBonusAt(Level(UpgradeType.AttackBoost)) : 1f;

    public static bool RollRefund()
    {
        var u = Instance;
        return u != null && u.RefundChance > 0f && Random.value < u.RefundChance;
    }

    // ---------------- Bag ----------------
    void RefillBag(UpgradeType? exclude)
    {
        bag.Clear();
        for (int i = 0; i < TypeCount; i++)
        {
            var t = (UpgradeType)i;
            if (exclude.HasValue && exclude.Value == t) continue;
            bag.Add(t);
        }
    }

    UpgradeType DrawFromBag()
    {
        int i = Random.Range(0, bag.Count);
        var t = bag[i];
        bag.RemoveAt(i);
        return t;
    }

    /// <summary>Draws two distinct options. Drawn entries leave the bag; it refills only once all six have appeared.</summary>
    public UpgradeType[] DrawOptions()
    {
        if (bag.Count == 0) RefillBag(null);
        var first = DrawFromBag();
        if (bag.Count == 0) RefillBag(first);
        var second = DrawFromBag();
        return new[] { first, second };
    }

    public void Apply(UpgradeType t)
    {
        int i = (int)t;
        Levels[i]++;
        Debug.Log($"[Upgrade] {t} is now level {Levels[i]}");
        Sfx.Play(SfxId.Select, 1f, 0f);
    }

    // ---------------- Automatic power-ups ----------------
    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.InputLocked) return;
        var human = gm.HumanPlayer;
        if (human == null || human.IsDead) return;
        var gold = new Color(1f, 0.85f, 0.2f);

        int hl = Levels[(int)UpgradeType.BurstHealing];
        if (hl > 0)
        {
            // Step 1: first frame after acquiring -> start a full countdown. Nothing fires on pickup.
            if (!healStarted)
            {
                healStarted = true;
                healTimer = HealCooldownAt(hl);
                Debug.Log($"[BurstHealing] acquired; first heal possible in {healTimer:0}s (and only if HP is missing)");
            }

            // Step 2: count down (scaled time, so it freezes while paused).
            if (healTimer > 0f) healTimer -= Time.deltaTime;

            // Step 3: when the countdown is done, wait until the human is actually hurt, then heal.
            bool needsHeal = human.Health < human.MaxHealth - 0.01f;
            if (healTimer <= 0f && needsHeal)
            {
                float before = human.Health;
                human.Heal(human.MaxHealth * HealPercentAt(hl));
                Debug.Log($"[BurstHealing] healed {human.Health - before:0.#} HP ({before:0.#} -> {human.Health:0.#} / {human.MaxHealth:0.#})");

                // Step 4: restart the countdown; Cooldown Refund may skip it (small re-trigger delay so it can't spam).
                bool freeHeal = RollRefund();
                healTimer = freeHeal ? 1f : HealCooldownAt(hl);
                if (freeHeal) Fx.Popup(human.transform.position + Vector3.up * 1.3f, "FREE!", gold);
            }
        }

        int bl = Levels[(int)UpgradeType.AttackBoost];
        if (bl > 0 && !BoostActive && boostCd.IsReady)
        {
            boostActiveUntil = Time.time + BoostDurationAt(bl);
            bool freeBoost = boostCd.Start(BoostCooldownAt(bl), 0f);
            Sfx.Play(SfxId.Boost);
            Fx.Ring(human.transform.position, 0.5f, 3f, gold, 0.4f);
            if (gm.GhostPlayer != null) Fx.Ring(gm.GhostPlayer.transform.position, 0.5f, 3f, gold, 0.4f);
            if (freeBoost) Fx.Popup(human.transform.position + Vector3.up * 1.3f, "FREE!", gold);
        }
    }

    // ---------------- UI text ----------------
    public static string NameOf(UpgradeType t)
    {
        switch (t)
        {
            case UpgradeType.PiercingAttacks: return "Piercing Attacks";
            case UpgradeType.BurstHealing: return "Burst Healing";
            case UpgradeType.AttackBoost: return "Attack Boost";
            case UpgradeType.ChargedAttack: return "Charged Attack";
            case UpgradeType.CooldownRefund: return "Cooldown Refund";
            default: return "Secondary Auto-Attack";
        }
    }

    public static string DescriptionOf(UpgradeType t)
    {
        switch (t)
        {
            case UpgradeType.PiercingAttacks: return "Projectiles from both players pass through multiple enemies.";
            case UpgradeType.BurstHealing: return "Automatically restores human health on a long timer.";
            case UpgradeType.AttackBoost: return "Periodically boosts attack power for both players.";
            case UpgradeType.ChargedAttack: return $"Hold Space (human) or Left Click (ghost) for {ChargeTime:0.##}s, release to fire a powerful shot.";
            case UpgradeType.CooldownRefund: return "Abilities and power-ups sometimes don't go on cooldown when used.";
            default: return "A weaker projectile fires in the opposite direction of your main shot.";
        }
    }

    public static string EffectAt(UpgradeType t, int lv)
    {
        switch (t)
        {
            case UpgradeType.PiercingAttacks: return $"Pierce up to {PierceHitsAt(lv)} enemies";
            case UpgradeType.BurstHealing: return $"Heal {HealPercentAt(lv) * 100f:0}% max HP every {HealCooldownAt(lv):0}s";
            case UpgradeType.AttackBoost: return $"+{BoostBonusAt(lv) * 100f:0}% attack for {BoostDurationAt(lv):0}s every {BoostCooldownAt(lv):0}s";
            case UpgradeType.ChargedAttack: return $"Charged shot: {ChargeDamageMultAt(lv):0.#}x damage, {ChargeSizeMultAt(lv):0.#}x size";
            case UpgradeType.CooldownRefund: return $"{RefundChanceAt(lv) * 100f:0}% chance to skip a cooldown";
            default: return $"Rear shot at {SecondaryFractionAt(lv) * 100f:0}% damage";
        }
    }

    public static Color ColorOf(UpgradeType t)
    {
        switch (t)
        {
            case UpgradeType.PiercingAttacks: return new Color(0.4f, 0.8f, 1f);
            case UpgradeType.BurstHealing: return new Color(0.4f, 1f, 0.5f);
            case UpgradeType.AttackBoost: return new Color(1f, 0.6f, 0.2f);
            case UpgradeType.ChargedAttack: return new Color(1f, 0.9f, 0.3f);
            case UpgradeType.CooldownRefund: return new Color(0.8f, 0.5f, 1f);
            default: return new Color(1f, 0.4f, 0.6f);
        }
    }

    public string StatusText(UpgradeType t)
    {
        int lv = Levels[(int)t];
        switch (t)
        {
            case UpgradeType.PiercingAttacks: return $"Pierce {PierceHitsAt(lv)} enemies";
            case UpgradeType.BurstHealing:
                if (!healStarted || healTimer > 0f) return $"{Mathf.CeilToInt(healStarted ? healTimer : HealCooldownAt(lv))}s";
                return "Ready (waits for damage)";
            case UpgradeType.AttackBoost:
                if (BoostActive) return $"ACTIVE {Mathf.CeilToInt(BoostTimeLeft)}s";
                return boostCd.IsReady ? "Ready" : $"{Mathf.CeilToInt(boostCd.Remaining)}s";
            case UpgradeType.ChargedAttack: return $"{ChargeDamageMultAt(lv):0.#}x damage";
            case UpgradeType.CooldownRefund: return $"{RefundChanceAt(lv) * 100f:0}% chance";
            default: return $"{SecondaryFractionAt(lv) * 100f:0}% damage";
        }
    }
}