using System.Collections.Generic;
using UnityEngine;

// Opposite to TetherBreaker, this provides the damage and knockback effects to the Enemies colliding with the Tether. Also provides a small cooldown to ensure Enemies cannot be hit more than once within a short period of time. -E.M

public class TetherDamageDealer : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 10f;

    [Tooltip("Seconds before the same enemy can be hit again by ANY tether hitbox.")]
    [SerializeField] private float hitCooldown = 0.5f;

    [Header("Knockback")]
    [Tooltip("How far the enemy is shoved away from the rope, in meters.")]
    [SerializeField] private float knockbackDistance = 3f;

    [Tooltip("If the enemy is dead-center on the rope and no clear push direction exists, shove them along the hitbox's sideways axis instead.")]
    [SerializeField] private bool useSidewaysFallback = true;

    [Header("Filtering")]
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Debug")]
    [Tooltip("Log every trigger contact while diagnosing setup. Turn off when working.")]
    [SerializeField] private bool debugLogging = false;

    // Cached tether reference.
    private AnchorTether cachedTether;
    private AnchorTether Tether
    {
        get
        {
            if (cachedTether == null)
                cachedTether = GetComponentInParent<AnchorTether>();
            return cachedTether;
        }
    }

    // Cached player movement, resolved from the same root as the tether. Used to stun the player on a Golem break.
    private PlayerMovement cachedPlayerMovement;
    private PlayerMovement PlayerMovementRef
    {
        get
        {
            if (cachedPlayerMovement == null && Tether != null)
                cachedPlayerMovement = Tether.GetComponentInParent<PlayerMovement>();
            return cachedPlayerMovement;
        }
    }

    // Cached player anchor, resolved from the same root as the tether. The break MUST go through this so isTethered clears and OnTetherReleased fires — otherwise overcharge/charge stay live on a dead rope.
    private PlayerAnchor cachedPlayerAnchor;
    private PlayerAnchor PlayerAnchorRef
    {
        get
        {
            if (cachedPlayerAnchor == null && Tether != null)
                cachedPlayerAnchor = Tether.GetComponentInParent<PlayerAnchor>();
            return cachedPlayerAnchor;
        }
    }

    // Shared across ALL hitboxes on the rope, so 8 adjacent capsules sweeping through one enemy count as a single hit instead of eight.
    private static readonly Dictionary<Collider, float> lastHitTimes = new();

    // Tracks which breakers are being actively contacted THIS physics step, shared across all hitboxes so multiple capsules touching one Golem don't each reset each other. Used to detect contact-loss for dwell reset.
    private static readonly HashSet<TetherBreaker> contactedThisStep = new();
    private static readonly HashSet<TetherBreaker> contactedLastStep = new();

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    // OnTriggerStay covers the case where an enemy walks INTO a stationary rope (Enter alone would only fire once and then cooldown-gate forever).
    private void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    private void TryHit(Collider other)
    {
        if (debugLogging)
            Debug.Log($"[TetherHit] touched '{other.name}' (tag={other.tag}, layer={LayerMask.LayerToName(other.gameObject.layer)})");

        //Block damage if the tether is broken or the anchor is broken
        var anchor = Tether.CurrentAnchor;
        if (anchor == null || anchor.Element == AnchorElement.Broken)
            return;

        if (!other.CompareTag(enemyTag)) return;

        // --- BREAKER DWELL PATH (runs BEFORE the hit-cooldown gate) ---
        // Dwell must accumulate every physics step of contact, so it can't sit behind the per-enemy hitCooldown that gates discrete damage ticks. We handle the breaker here and return early.
        TetherBreaker breaker = other.GetComponentInParent<TetherBreaker>();
        if (breaker != null)
        {
            HandleBreakerContact(breaker, other);
            return;
        }

        // --- NORMAL DAMAGE / KNOCKBACK PATH (unchanged) ---
        // Per-enemy cooldown shared across all rope hitboxes
        if (lastHitTimes.TryGetValue(other, out float lastTime)
            && Time.time - lastTime < hitCooldown)
            return;

        lastHitTimes[other] = Time.time;

        ApplyDamage(other);
        ApplyKnockback(other);
    }

    // Feeds sustained-contact time into the breaker. When its threshold is met, severs the tether and reels it in. Marks the breaker as contacted this step so LateUpdate can detect contact-loss and reset dwell.
    private void HandleBreakerContact(TetherBreaker breaker, Collider other)
    {
        contactedThisStep.Add(breaker);

        if (!breaker.CanBreakTether) return;

        if (breaker.AddContact(Time.fixedDeltaTime))
        {
            // Route the break through PlayerAnchor, NOT AnchorTether directly.
            PlayerAnchor pa = PlayerAnchorRef;
            if (pa != null)
            {
                pa.ReleaseTether(playReel: true);
                breaker.NotifyBrokeTether();
            }
            else
            {
                // Fallback: no PlayerAnchor found (shouldn't happen in normal setup). Preserve the old visual-only behaviour rather than silently doing nothing, and warn so it's caught.
                if (Tether != null)
                {
                    Tether.ReelInAndBreak();
                    breaker.NotifyBrokeTether();
                }
                if (debugLogging)
                    Debug.LogWarning("[TetherDamageDealer] Break could not find PlayerAnchor up from the tether — tether state may not have cleared.");
            }

            // Golem-break-only: freeze the player's movement for the breaker's configured duration.
            // This fires ONLY here, so manual detach / range / LOS releases never stun the player.
            if (breaker.PlayerStunDuration > 0f)
            {
                PlayerMovement pm = PlayerMovementRef;
                if (pm != null)
                    pm.ApplyStun(breaker.PlayerStunDuration);
                else if (debugLogging)
                    Debug.LogWarning("[TetherDamageDealer] Break stun requested but no PlayerMovement found up from the tether.");
            }

            // Optional selective effects on the breaking hit, matching the old behaviour.
            if (breaker.TakeContactDamage)
                ApplyDamage(other);
            if (breaker.TakeKnockback)
                ApplyKnockback(other);
        }
    }

    // At end of each physics step, any breaker that was contacted last step but NOT this step has lost contact -> reset its dwell to zero. Runs once globally (guarded) rather than per-hitbox.
    private static bool sweptThisStep;
    private void FixedUpdate()
    {
        // Only one hitbox needs to run the sweep; the sets are static/shared.
        if (sweptThisStep) return;
        sweptThisStep = true;

        foreach (var breaker in contactedLastStep)
        {
            if (breaker != null && !contactedThisStep.Contains(breaker))
                breaker.ResetContact();
        }

        contactedLastStep.Clear();
        foreach (var b in contactedThisStep)
            contactedLastStep.Add(b);
        contactedThisStep.Clear();
    }

    private void LateUpdate()
    {
        // Reset the per-step sweep guard so the next FixedUpdate runs it again.
        sweptThisStep = false;
    }

    private void ApplyDamage(Collider other)
    {
        // Health implements IDamageable; using the interface keeps this working for anything damageable, not just enemies with the Health component.
        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDmg(damage);
        }
        else if (other.attachedRigidbody != null
                 && other.attachedRigidbody.TryGetComponent(out IDamageable rbDamageable))
        {
            // Collider might be on a child hitbox object; check the rigidbody root.
            rbDamageable.TakeDmg(damage);
        }
    }

    private void ApplyKnockback(Collider other)
    {
        EnemyKnockback knockback = other.GetComponentInParent<EnemyKnockback>();
        if (knockback == null) return;

        Vector3 pushDir = ComputePushDirection(other);
        knockback.ApplyKnockback(pushDir, knockbackDistance);
    }

    // Direction that pushes the enemy off the rope: from the closest point on this hitbox's segment axis to the enemy, flattened to the horizontal plane.
    private Vector3 ComputePushDirection(Collider enemy)
    {
        // The hitbox capsule is aligned along local Z (down the rope), so the rope's line through this segment is transform.forward through center.
        Vector3 segmentAxis = transform.forward;
        Vector3 toEnemy = enemy.bounds.center - transform.position;

        // Remove the along-rope component so we push perpendicular to the rope, straight off of it, instead of along it.
        Vector3 perpendicular = toEnemy - Vector3.Project(toEnemy, segmentAxis);
        perpendicular.y = 0f;

        if (perpendicular.sqrMagnitude > 1e-4f)
            return perpendicular.normalized;

        // Enemy is dead-center on the rope axis - no natural push direction.
        if (useSidewaysFallback)
        {
            Vector3 side = Vector3.Cross(Vector3.up, segmentAxis);
            side.y = 0f;
            if (side.sqrMagnitude > 1e-4f)
                return side.normalized;
        }

        // Last resort: push away from the hitbox center horizontally.
        toEnemy.y = 0f;
        return toEnemy.sqrMagnitude > 1e-4f ? toEnemy.normalized : Vector3.forward;
    }
}