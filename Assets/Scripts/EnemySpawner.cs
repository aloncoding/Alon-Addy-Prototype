using UnityEngine;

/// <summary>Spawns enemies just outside the camera's CURRENT visible rectangle and scales difficulty over time.</summary>
public class EnemySpawner : MonoBehaviour
{
    float accumulator;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.InputLocked || DynamicCamera.Instance == null) return;
        var t = gm.tuning;

        float minutes = gm.ElapsedTime / 60f;
        float rate = t.baseSpawnRate * (1f + 0.3f * minutes + 0.02f * minutes * minutes);
        accumulator += rate * Time.deltaTime;

        int guard = 0;
        while (accumulator >= 1f && guard++ < 10)
        {
            accumulator -= 1f;
            if (Enemy.All.Count < t.maxEnemies) SpawnOne(minutes);
        }
        accumulator = Mathf.Min(accumulator, 2f);
    }

    void SpawnOne(float minutes)
    {
        Enemy.Spawn(PickSpawnPoint(), RollStats(minutes));
    }

    Vector2 PickSpawnPoint()
    {
        var gm = GameManager.Instance;
        var vr = DynamicCamera.Instance.VisibleRect;
        float m = gm.tuning.spawnMargin;
        var r = Rect.MinMaxRect(vr.xMin - m, vr.yMin - m, vr.xMax + m, vr.yMax + m);

        Vector2 p = Vector2.zero;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            float w = r.width, h = r.height;
            float d = Random.value * 2f * (w + h);
            if (d < w) p = new Vector2(r.xMin + d, r.yMax);
            else if (d < w + h) p = new Vector2(r.xMax, r.yMax - (d - w));
            else if (d < 2f * w + h) p = new Vector2(r.xMax - (d - w - h), r.yMin);
            else p = new Vector2(r.xMin, r.yMin + (d - 2f * w - h));

            bool farFromHuman = gm.HumanPlayer == null || Vector2.Distance(p, gm.HumanPlayer.transform.position) > 3f;
            bool farFromGhost = gm.GhostPlayer == null || Vector2.Distance(p, gm.GhostPlayer.transform.position) > 3f;
            if (farFromHuman && farFromGhost) return p;
        }
        return p;
    }

    /// <summary>Gradual scaling: health, speed, damage and stat variance all rise smoothly with minutes survived.</summary>
    Enemy.Stats RollStats(float m)
    {
        var t = GameManager.Instance.tuning;
        float variance = Mathf.Min(0.6f, 0.25f + 0.03f * m);
        float healthMult = 1f + 0.25f * m + 0.01f * m * m;
        float speedMult = Mathf.Min(1.8f, 1f + 0.04f * m);
        float damageMult = 1f + 0.08f * m;

        float health = t.baseEnemyHealth * healthMult * Mathf.Max(0.4f, 1f + Random.Range(-variance, variance * 1.6f));
        float speed = t.baseEnemySpeed * speedMult * (1f + Random.Range(-variance * 0.5f, variance * 0.5f));
        float damage = t.baseEnemyDamage * damageMult * (1f + Random.Range(-variance * 0.5f, variance * 0.5f));

        float ratio = health / t.baseEnemyHealth;
        float size = Mathf.Clamp(0.5f + 0.6f * Mathf.Sqrt(ratio), 0.6f, 2.2f);   // bigger = tankier
        float score = (ratio + speed / t.baseEnemySpeed + damage / t.baseEnemyDamage) / 3f;
        int xp = Mathf.Max(1, Mathf.RoundToInt(score * Random.Range(0.6f, 1.8f))); // higher stats => more XP on average
        Color color = Color.Lerp(new Color(1f, 0.55f, 0.5f), new Color(0.65f, 0.1f, 0.2f), Mathf.InverseLerp(0.6f, 2.2f, size));

        return new Enemy.Stats { health = health, speed = speed, damage = damage, size = size, xp = xp, color = color };
    }
}