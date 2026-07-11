using UnityEngine;

// Tether Breaker script for the Rock Golem Enemy - if it makes contact with the Tether at any point, it immediately breaks the Tether. -E.M

public class TetherBreaker : MonoBehaviour
{
    [Tooltip("If true, the breaker also takes damage on the hit that broke the tether. Off = pure hazard.")]
    [SerializeField] private bool takeContactDamage = false;

    [Tooltip("If true, the breaker is also shoved on the hit that broke the tether. Off = it stands its ground.")]
    [SerializeField] private bool takeKnockback = false;

    [Tooltip("Seconds after breaking a tether during which this enemy won't break another. Prevents insta-rebreak.")]
    [SerializeField] private float contactCooldown = 0.5f;

    public bool TakeContactDamage => takeContactDamage;
    public bool TakeKnockback => takeKnockback;

    private float nextBreakAllowedTime;

    // True if this breaker can currently sever a tether.
    public bool CanBreakTether => Time.time >= nextBreakAllowedTime;

    // Called by the dealer when this breaker severs a tether.
    public void NotifyBrokeTether()
    {
        nextBreakAllowedTime = Time.time + contactCooldown;
    }
}
