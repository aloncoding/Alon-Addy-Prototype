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

    [Header("Progression")]
    public float xpThreshold = 25f;
    [Tooltip("Extra XP required per level gained. 0 = flat threshold (GDD baseline).")]
    public float xpThresholdGrowthPerLevel = 0f;

    [Header("Run")]
    public float restartDelayAfterDeath = 2f;
    public bool showDamageNumbers = true;
}