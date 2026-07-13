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

    // Shared across ALL hitboxes on the rope, so 8 adjacent capsules sweeping through one enemy count as a single hit instead of eight.
    private static readonly Dictionary<Collider, float> lastHitTimes = new();

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

        if (!other.CompareTag(enemyTag)) return;

        // Per-enemy cooldown shared across all rope hitboxes
        if (lastHitTimes.TryGetValue(other, out float lastTime)
            && Time.time - lastTime < hitCooldown)
            return;

        lastHitTimes[other] = Time.time;

        // If the enemy is marked as a breaker, sever the tether and skip normal damage/knockback (unless the breaker opts into taking them).
        TetherBreaker breaker = other.GetComponentInParent<TetherBreaker>();
        if (breaker != null && breaker.CanBreakTether)
        {
            if (Tether != null)
            {
                Tether.BreakTether();
                breaker.NotifyBrokeTether();
            }

            // Most breakers are hazards that don't get damaged by the break itself. If they've opted in, fall through; otherwise stop here.
            if (!breaker.TakeContactDamage && !breaker.TakeKnockback)
                return;

            // Selective fall-through: apply only what the breaker opts into.
            if (breaker.TakeContactDamage)
                ApplyDamage(other);
            if (breaker.TakeKnockback)
                ApplyKnockback(other);
            return;
        }

        // Damage 
        ApplyDamage(other);

        // Knockback 
        ApplyKnockback(other);
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