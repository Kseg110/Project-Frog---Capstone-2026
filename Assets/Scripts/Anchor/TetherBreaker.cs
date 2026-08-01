using UnityEngine;

// Tether Breaker for the Rock Golem. It no longer breaks on contact instantly - it requires sustained contact for `requiredContactTime` seconds before the tether is severed. 
// Contact time is fed in each physics step by TetherDamageDealer while a rope hitbox overlaps this enemy, and reset to zero the moment contact is lost. -E.M
public class TetherBreaker : MonoBehaviour
{
    [Tooltip("Seconds of continuous contact required before the tether breaks.")]
    [SerializeField] private float requiredContactTime = 3f;

    [Tooltip("Seconds the player's movement is frozen when THIS breaker severs the tether. 0 = no stun.")]
    [SerializeField] private float playerStunDuration = 0.5f;

    [Tooltip("If true, the breaker also takes damage on the hit that broke the tether. Off = pure hazard.")]
    [SerializeField] private bool takeContactDamage = false;
    [Tooltip("If true, the breaker is also shoved on the hit that broke the tether. Off = it stands its ground.")]
    [SerializeField] private bool takeKnockback = false;
    [Tooltip("Seconds after breaking a tether during which this enemy won't break another. Prevents insta-rebreak.")]
    [SerializeField] private float contactCooldown = 0.5f;

    public bool TakeContactDamage => takeContactDamage;
    public bool TakeKnockback => takeKnockback;
    public float PlayerStunDuration => playerStunDuration;

    private float nextBreakAllowedTime;
    private float accumulatedContact;

    // True if this breaker can currently sever a tether (cooldown elapsed).
    public bool CanBreakTether => Time.time >= nextBreakAllowedTime;

    // Fraction of the required dwell currently accumulated (0-1). Handy for a fill/flash VFX later.
    public float ContactProgress =>
        requiredContactTime <= 0f ? 1f : Mathf.Clamp01(accumulatedContact / requiredContactTime);

    // Feeds contact time in. Returns true the moment the threshold is crossed.
    // TetherDamageDealer calls this every FixedUpdate a rope hitbox is overlapping this enemy.
    public bool AddContact(float deltaTime)
    {
        if (!CanBreakTether) return false;

        accumulatedContact += deltaTime;
        return accumulatedContact >= requiredContactTime;
    }

    // Called by the dealer the instant contact is lost.
    public void ResetContact()
    {
        accumulatedContact = 0f;
    }

    // Called by the dealer when this breaker actually severs a tether. Starts the cooldown and clears dwell.
    public void NotifyBrokeTether()
    {
        nextBreakAllowedTime = Time.time + contactCooldown;
        accumulatedContact = 0f;
    }
}