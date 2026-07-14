using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Similar to the PlayerTakeDamage script, this reuses the knockback logic to allow for Enemies such as the Croc and Skeletal Frog to be knocked back upon collision with the Tether. -E.M
[RequireComponent(typeof(Rigidbody))]
public class EnemyKnockback : MonoBehaviour
{
    [Header("Knockback")]
    [Tooltip("Knockback speed in meters per second.")]
    [SerializeField] private float knockbackSpeed = 20f;

    [Tooltip("Power of the ease-out curve. Higher = snappier start, softer end.")]
    [SerializeField] private float knockbackEasePower = 2f;

    [Tooltip("Multiplier on incoming knockback distance. Use for heavy enemies (0.5) or light ones (1.5).")]
    [SerializeField] private float knockbackResistance = 1f;

    [Header("Projectile Knockback")]
    [Tooltip("How far a projectile impact shoves this enemy, before resistance is applied.")]
    [SerializeField] private float projectileKnockbackDistance = 2f;

    [Tooltip("If true, knockback direction is the projectile's travel direction. If false, it's pushed away from the projectile's position.")]
    [SerializeField] private bool useProjectileTravelDirection = true;

    [Header("Collision")]
    [Tooltip("Layers the enemy collides with while being knocked back (walls, terrain).")]
    [SerializeField] private LayerMask collisionLayers;

    [Tooltip("Optional capsule used for collision-safe movement. Auto-found if left empty.")]
    [SerializeField] private CapsuleCollider capsule;

    public bool IsBeingKnockedBack { get; private set; }

    private Rigidbody rb;
    private NavMeshAgent agent;
    private EnemyBase enemyBase;
    private Coroutine knockbackCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        enemyBase = GetComponent<EnemyBase>();

        if (capsule == null)
            capsule = GetComponentInChildren<CapsuleCollider>();
    }

    // --- Projectile hooks -------------------------------------------------
    // Covers both setups: projectile collider marked as a trigger, or a solid collider.
    private void OnTriggerEnter(Collider other)
    {
        TryProjectileKnockback(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryProjectileKnockback(collision.collider);
    }

    // Looks for an IProjectile on the thing we touched. Knockback only, no damage,
    // damage is handled by whatever else listens for the projectile hit. -E.M
    private void TryProjectileKnockback(Collider other)
    {
        if (other == null) return;

        // GetComponentInParent catches projectiles whose collider sits on a child object.
        IProjectile projectile = other.GetComponentInParent<IProjectile>();
        if (projectile == null) return;

        Vector3 direction;

        if (useProjectileTravelDirection && other.attachedRigidbody != null &&
            other.attachedRigidbody.linearVelocity.sqrMagnitude > 1e-6f)
        {
            // Shove the enemy the way the projectile was already flying.
            direction = other.attachedRigidbody.linearVelocity;
        }
        else
        {
            // Fallback: push straight away from the impact point.
            direction = transform.position - other.transform.position;
        }

        ApplyKnockback(direction, projectileKnockbackDistance);
    }
    // ---------------------------------------------------------------------

    // Knocks this Enemy object back. Direction is flattened to the horizontal plane and normalized internally, so callers can pass rough vectors.
    public void ApplyKnockback(Vector3 direction, float distance)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 1e-6f) return;

        float finalDistance = distance * knockbackResistance;
        if (finalDistance <= 0f) return;

        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine = StartCoroutine(
            KnockbackRoutine(direction.normalized, finalDistance));
    }

    private IEnumerator KnockbackRoutine(Vector3 dir, float distance)
    {
        IsBeingKnockedBack = true;

        // Pause pathfinding so the Enemy doesn't fight the shove.
        bool hadAgent = agent != null && agent.enabled;
        if (hadAgent)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
        }

        Vector3 start = rb.position;
        float duration = Mathf.Max(0.01f, distance / knockbackSpeed);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // Ease-out interpolation (same curve as the player's knockback)
            float easedT = 1f - Mathf.Pow(1f - t, knockbackEasePower);

            Vector3 desiredPosition = start + dir * (distance * easedT);
            Vector3 motion = desiredPosition - rb.position;

            if (capsule != null)
            {
                // Collision-safe movement, identical approach to the player.
                CollisionUtility.MoveWithCapsuleCollision(rb, capsule, motion, collisionLayers);
            }
            else
            {
                rb.MovePosition(rb.position + motion);
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Hand control back to the Enemy at the final position.
        if (hadAgent && agent != null)
        {
            agent.Warp(rb.position);   // snap Enemy's internal position to where we ended up
            agent.updatePosition = true;
            agent.isStopped = false;
        }

        // enemyBase?.SetStunned(false);
        IsBeingKnockedBack = false;
        knockbackCoroutine = null;
    }
}