using UnityEngine;

/// <summary>
/// Every balance number lives here so you can tweak it from the GameManager inspector.
/// Upgrade scaling formulas live in UpgradeSystem.cs (static "...At(level)" functions).
/// </summary>
[System.Serializable]
public class GameTuning
{
    [Header("Human")]
    public float humanMaxHealth = 100f;
    public float humanMoveSpeed = 5.5f;
    public float humanHitInvulnerability = 0.35f;
    [Tooltip("Passive health regeneration (HP per second) while the game is running.")]
    public float humanRegenPerSecond = 1f;
    public float gunDamage = 10f;
    public float gunFireInterval = 0.55f;
    public float meleeDamage = 18f;
    public float meleeRadius = 2f;
    public float meleeKnockback = 8f;
    public float meleeCooldown = 0.45f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.16f;
    public float dashCooldown = 3f;
    [Tooltip("Brief invulnerability while dashing, so the dash works as an evasion tool.")]
    public float dashInvulnerability = 0.25f;

    [Header("Ghost")]
    public float ghostMaxSpeed = 16f;
    public float ghostDamage = 4f;
    public float ghostFireInterval = 0.7f;
    public float ghostSlowDuration = 2f;
    public float ghostWeakenDuration = 3f;
    public float scareRadius = 6f;
    public float scareDuration = 3f;
    public float scareCooldown = 30f;
    public float tauntRadius = 8f;
    public float tauntDuration = 5f;
    public float tauntCooldown = 45f;

    [Header("Enemies")]
    public int maxEnemies = 350;
    public float baseSpawnRate = 0.9f;
    public float spawnMargin = 2f;
    public float baseEnemyHealth = 20f;
    public float baseEnemySpeed = 2.2f;
    public float baseEnemyDamage = 8f;
    public float enemyAttackInterval = 2f;
    [Range(0f, 1f)] public float xpDropChance = 0.9f;
    [Tooltip("Difficulty stops increasing after this many minutes of survival time.")]
    public float maxDifficultyMinutes = 5f;
    [Tooltip("How far along the difficulty curve the game is when it hits max difficulty (in curve-minutes). " +
             "15 = reaches what used to be the 15-minute intensity at 5 minutes. Set equal to Max Difficulty Minutes to simply cap the old curve.")]
    public float curveMinutesAtMaxDifficulty = 15f;

    // ======================= POWER-UPS =======================
    // Level N value = base + perLevel x (N - 1). "Min/Max" values clamp the result.

    [Header("Power-Up: Piercing Attacks")]
    [Tooltip("How many enemies a projectile can hit at level 1.")]
    public int pierceHitsAtLevel1 = 3;
    public int pierceExtraHitsPerLevel = 1;

    [Header("Power-Up: Burst Healing")]
    [Tooltip("Fraction of max HP restored per heal at level 1 (0.2 = 20%).")]
    public float healBasePercent = 0.20f;
    public float healPercentPerLevel = 0.05f;
    [Tooltip("Seconds between heals at level 1.")]
    public float healBaseCooldown = 100f;
    public float healCooldownReductionPerLevel = 10f;
    public float healMinCooldown = 40f;

    [Header("Power-Up: Attack Boost")]
    [Tooltip("Attack power bonus at level 1 (0.2 = +20%).")]
    public float boostBaseBonus = 0.20f;
    public float boostBonusPerLevel = 0.10f;
    [Tooltip("How long the boost lasts (seconds) at level 1.")]
    public float boostBaseDuration = 10f;
    public float boostDurationPerLevel = 2f;
    [Tooltip("Seconds between boost activations at level 1 (counted from activation).")]
    public float boostBaseCooldown = 100f;
    public float boostCooldownReductionPerLevel = 10f;
    public float boostMinCooldown = 40f;

    [Header("Power-Up: Charged Attack")]
    [Tooltip("Seconds the attack input must be held before the shot is charged.")]
    public float chargeTime = 0.4f;
    [Tooltip("Damage multiplier of the charged shot at level 1.")]
    public float chargeBaseDamageMult = 3f;
    public float chargeDamageMultPerLevel = 1f;
    [Tooltip("Projectile size multiplier of the charged shot at level 1.")]
    public float chargeBaseSizeMult = 1.6f;
    public float chargeSizeMultPerLevel = 0.2f;

    [Header("Power-Up: Cooldown Refund")]
    [Tooltip("Chance (0-1) that an ability/power-up skips its cooldown at level 1.")]
    [Range(0f, 1f)] public float refundBaseChance = 0.10f;
    public float refundChancePerLevel = 0.05f;
    [Range(0f, 1f)] public float refundMaxChance = 0.75f;

    [Header("Power-Up: Secondary Auto-Attack")]
    [Tooltip("Rear shot damage as a fraction of the main shot at level 1 (0.5 = 50%).")]
    public float secondaryBaseFraction = 0.5f;
    public float secondaryFractionPerLevel = 0.1f;
    public float secondaryMaxFraction = 1f;

    [Header("Progression")]
    public float xpThreshold = 25f;
    [Tooltip("Extra XP required per level gained. 0 = flat threshold (GDD baseline).")]
    public float xpThresholdGrowthPerLevel = 0f;

    [Header("Run")]
    public float restartDelayAfterDeath = 2f;
    public bool showDamageNumbers = true;
}