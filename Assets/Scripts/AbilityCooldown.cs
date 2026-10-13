using UnityEngine;

/// <summary>
/// Cooldown timer with Cooldown Refund Chance built in.
/// Start() rolls the refund: on success the cooldown never begins (the effect already happened).
/// It never shortens or resets a cooldown that is already running.
/// </summary>
public class AbilityCooldown
{
    public float Duration { get; private set; }
    float readyAt;

    public bool IsReady => Time.time >= readyAt;
    public float Remaining => Mathf.Max(0f, readyAt - Time.time);
    public float Fraction => Duration <= 0f ? 0f : Mathf.Clamp01(Remaining / Duration);

    /// <returns>true if the cooldown was refunded (skipped).</returns>
    public bool Start(float duration, float retriggerDelayIfRefunded = 0f)
    {
        Duration = duration;
        if (UpgradeSystem.RollRefund())
        {
            readyAt = Time.time + retriggerDelayIfRefunded;
            return true;
        }
        readyAt = Time.time + duration;
        return false;
    }

    /// <summary>Starts a cooldown with no refund roll (used when a power-up is first acquired).</summary>
    public void ForceStart(float duration)
    {
        Duration = duration;
        readyAt = Time.time + duration;
    }
}