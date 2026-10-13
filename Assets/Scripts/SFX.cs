using UnityEngine;

public enum SfxId
{
    Shoot, GhostShoot, Hit, Melee, Dash, Scare, Taunt, Pickup, LevelUp, Select,
    Charged, Hurt, Heal, Boost, EnemyDie, Death, Free
}

/// <summary>Tiny static sound player. Clips are synthesized in ProceduralAssets; swap them for real audio if you like.</summary>
public static class Sfx
{
    static AudioSource source;
    static readonly float[] lastPlayed = new float[32];

    public static void Play(SfxId id, float volume = 1f, float minInterval = 0.03f)
    {
        if (source == null)
        {
            var go = new GameObject("Sfx");
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
        }
        float now = Time.unscaledTime;
        if (now - lastPlayed[(int)id] < minInterval) return;
        lastPlayed[(int)id] = now;
        source.PlayOneShot(Clip(id), volume * 0.5f);
    }

    static AudioClip Clip(SfxId id)
    {
        switch (id)
        {
            case SfxId.Shoot:      return ProceduralAssets.Tone("shoot", 700, 300, 0.07f, 0.25f, 1);
            case SfxId.GhostShoot: return ProceduralAssets.Tone("gshoot", 900, 1300, 0.08f, 0.2f, 0);
            case SfxId.Hit:        return ProceduralAssets.Tone("hit", 220, 90, 0.06f, 0.4f, 2);
            case SfxId.Melee:      return ProceduralAssets.Tone("melee", 300, 80, 0.14f, 0.5f, 2);
            case SfxId.Dash:       return ProceduralAssets.Tone("dash", 400, 1000, 0.12f, 0.3f, 0);
            case SfxId.Scare:      return ProceduralAssets.Tone("scare", 900, 120, 0.6f, 0.4f, 0);
            case SfxId.Taunt:      return ProceduralAssets.Tone("taunt", 150, 400, 0.45f, 0.45f, 1);
            case SfxId.Pickup:     return ProceduralAssets.Tone("pickup", 1760, 1760, 0.2f, 0.45f, 0);
            case SfxId.LevelUp:    return ProceduralAssets.Tone("levelup", 500, 1200, 0.4f, 0.4f, 0);
            case SfxId.Select:     return ProceduralAssets.Tone("select", 600, 900, 0.1f, 0.3f, 0);
            case SfxId.Charged:    return ProceduralAssets.Tone("charged", 1000, 1800, 0.18f, 0.4f, 0);
            case SfxId.Hurt:       return ProceduralAssets.Tone("hurt", 200, 60, 0.2f, 0.5f, 1);
            case SfxId.Heal:       return ProceduralAssets.Tone("heal", 600, 1000, 0.35f, 0.3f, 0);
            case SfxId.Boost:      return ProceduralAssets.Tone("boost", 300, 900, 0.3f, 0.35f, 1);
            case SfxId.EnemyDie:   return ProceduralAssets.Tone("edie", 300, 100, 0.1f, 0.3f, 2);
            case SfxId.Death:      return ProceduralAssets.Tone("death", 400, 50, 0.9f, 0.5f, 1);
            default:               return ProceduralAssets.Tone("free", 1200, 1600, 0.1f, 0.25f, 0);
        }
    }
}