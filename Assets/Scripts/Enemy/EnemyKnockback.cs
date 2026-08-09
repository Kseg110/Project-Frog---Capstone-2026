using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Enemy knockback with collision-safe movement.
// Uses CollisionUtility so enemies cannot be pushed through walls/objects.
[RequireComponent(typeof(Rigidbody))]
public class EnemyKnockback : MonoBehaviour
{
    [Header("Knockback")]
    [Tooltip("Knockback speed in meters per second.")]
    [SerializeField] private float knockbackSpeed = 20f;

    [Tooltip("Power of the ease-out curve.")]
    [SerializeField] private float knockbackEasePower = 2f;

    [Tooltip("Multiplier on incoming knockback distance.")]
    [SerializeField] private float knockbackResistance = 1f;


    [Header("Projectile Knockback")]
    [SerializeField] private float projectileKnockbackDistance = 2f;

    [SerializeField] private bool useProjectileTravelDirection = true;

    [Header("Debug")]
    [Tooltip("Temporary multiplier for knockback — set very high to make knockback obvious while debugging.")]
    [SerializeField] private float debugKnockbackMultiplier = 50f;


    [Header("Collision")]
    [Tooltip("Layers the enemy is blocked by while being knocked back.")]
    [SerializeField] private LayerMask collisionLayers;


    [Tooltip("Capsule used for collision-safe movement.")]
    [SerializeField] private CapsuleCollider capsule;

    [Header("Fallback capsule (used when no CapsuleCollider assigned)")]
    [Tooltip("Height used for fallback capsule casts when a CapsuleCollider is not assigned.")]
    [SerializeField] private float fallbackCapsuleHeight = 1.6f;
    [Tooltip("Radius used for fallback capsule casts when a CapsuleCollider is not assigned.")]
    [SerializeField] private float fallbackCapsuleRadius = 0.3f;



    public bool IsBeingKnockedBack { get; private set; }



    private Rigidbody rb;
    private NavMeshAgent agent;
    private Coroutine knockbackCoroutine;

    // new: movement component reference so we can disable local movement logic while knocked back
    private MovementComponent movementComp;



    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        agent = GetComponent<NavMeshAgent>();

        movementComp = GetComponent<MovementComponent>();

        if (capsule == null)
            capsule = GetComponentInChildren<CapsuleCollider>();
    }



    // Projectile collision support

    private void OnTriggerEnter(Collider other)
    {
        TryProjectileKnockback(other);
    }


    private void OnCollisionEnter(Collision collision)
    {
        TryProjectileKnockback(collision.collider);
    }



    private void TryProjectileKnockback(Collider other)
    {
        if (other == null)
            return;


        IProjectile projectile =
            other.GetComponentInParent<IProjectile>();


        if (projectile == null)
            return;



        Vector3 direction;



        if (useProjectileTravelDirection &&
            other.attachedRigidbody != null &&
            other.attachedRigidbody.linearVelocity.sqrMagnitude > 0.001f)
        {
            direction =
                other.attachedRigidbody.linearVelocity;
        }
        else
        {
            direction =
                transform.position -
                other.transform.position;
        }



        ApplyKnockback(
            direction,
            projectileKnockbackDistance
        );
    }




    public void ApplyKnockback(
        Vector3 direction,
        float distance)
    {
        direction.y = 0f;


        if (direction.sqrMagnitude < 0.000001f)
            return;


        // increased final distance includes debug multiplier so it's visually obvious while debugging
        float finalDistance =
            distance *
            knockbackResistance *
            Mathf.Max(1f, debugKnockbackMultiplier);


        if (finalDistance <= 0f)
            return;

        Debug.Log($"[EnemyKnockback] ApplyKnockback on '{name}' dir={direction.normalized} requested={distance:F2} resistance={knockbackResistance:F2} debugMult={debugKnockbackMultiplier:F2} final={finalDistance:F2}", this);


        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);



        knockbackCoroutine =
            StartCoroutine(
                KnockbackRoutine(
                    direction.normalized,
                    finalDistance
                ));
    }






    private IEnumerator KnockbackRoutine(
        Vector3 dir,
        float distance)
    {
        IsBeingKnockedBack = true;

        // disable local movement so other scripts don't fight our forced movement
        if (movementComp != null)
            movementComp.SetMovementEnabled(false);

        bool hadAgent =
            agent != null &&
            agent.enabled;

        // DEBUG: start info
        Debug.Log($"[EnemyKnockback] KnockbackRoutine START on '{name}' dir={dir} distance={distance:F2} hadAgent={hadAgent}", this);

        if (hadAgent)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
        }



        float duration =
            Mathf.Max(
                0.01f,
                distance / knockbackSpeed
            );



        float elapsed = 0f;



        Vector3 start =
            rb.position;



        while (elapsed < duration)
        {
            float t =
                elapsed / duration;



            float easedT =
                1f -
                Mathf.Pow(
                    1f - t,
                    knockbackEasePower
                );



            Vector3 target =
                start +
                dir *
                (distance * easedT);



            Vector3 motion =
                target -
                rb.position;



            if (motion.sqrMagnitude > 0.000001f)
            {
                Vector3 oldPosition =
                    rb.position;



                if (capsule != null)
                {
                    // If Rigidbody is kinematic we must move the transform directly (MoveWithCapsuleCollision uses rb.MovePosition internally).
                    if (rb.isKinematic)
                    {
                        // compute capsule start/end in world space
                        CollisionUtility.GetCapsule(rb, capsule, out Vector3 capStart, out Vector3 capEnd);
                        Vector3 testStart = capStart + motion;
                        Vector3 testEnd = capEnd + motion;

                        // Check overlap at target; if clear, move transform; else try small incremental steps.
                        Collider[] overlaps = Physics.OverlapCapsule(testStart, testEnd, capsule.radius, collisionLayers, QueryTriggerInteraction.Ignore);
                        if (!TryGetBlockingOverlap(overlaps, out Collider blocking))
                        {
                            transform.position += motion;
                        }
                        else
                        {
                            Debug.Log($"[EnemyKnockback] Overlap blocked by '{blocking?.name}' (root='{blocking?.transform?.root?.name}', attachedRb={(blocking?.attachedRigidbody != null ? blocking.attachedRigidbody.name : "null")})", this);
                            bool moved = false;
                            float remaining = motion.magnitude;
                            float step = Mathf.Min(0.5f, remaining);
                            for (int s = 0; s < 6 && step > 0.001f; s++)
                            {
                                Vector3 stepMotion = motion.normalized * step;
                                Vector3 sStart = capStart + stepMotion;
                                Vector3 sEnd = capEnd + stepMotion;
                                Collider[] ov = Physics.OverlapCapsule(sStart, sEnd, capsule.radius, collisionLayers, QueryTriggerInteraction.Ignore);
                                if (!TryGetBlockingOverlap(ov, out Collider stepBlocking))
                                {
                                    transform.position += stepMotion;
                                    moved = true;
                                    break;
                                }
                                else
                                {
                                    Debug.Log($"[EnemyKnockback] incremental step blocked by '{stepBlocking?.name}'", this);
                                }
                                step *= 0.5f;
                            }

                            if (!moved)
                            {
                                Debug.Log($"[EnemyKnockback] Knockback blocked by capsule overlap on '{name}' — stopping (blocking '{blocking?.name}').", this);
                                break;
                            }
                        }
                    }
                    else
                    {
                        CollisionUtility.MoveWithCapsuleCollision(
                            rb,
                            capsule,
                            motion,
                            collisionLayers
                        );
                    }
                }
                else
                {
                    // Backup collision check using a reasonable capsule height so we don't immediately hit ground.
                    float capHeight = Mathf.Max(0.01f, fallbackCapsuleHeight);
                    float capRadius = Mathf.Max(0.01f, fallbackCapsuleRadius);

                    Vector3 capsuleTop = rb.position + Vector3.up * (capHeight / 2f);
                    Vector3 capsuleBottom = rb.position - Vector3.up * (capHeight / 2f);

                    if (!Physics.CapsuleCast(
                        capsuleTop,
                        capsuleBottom,
                        capRadius,
                        motion.normalized,
                        out RaycastHit hit,
                        motion.magnitude,
                        collisionLayers,
                        QueryTriggerInteraction.Ignore))
                    {
                        // No blocking hit -> move
                        if (rb.isKinematic)
                            transform.position += motion;
                        else
                            rb.MovePosition(rb.position + motion);
                    }
                    else
                    {
                        // If Rigidbody is kinematic, try a few small incremental steps to allow a nudge/slide.
                        if (rb.isKinematic)
                        {
                            bool moved = false;
                            float remaining = motion.magnitude;
                            float step = Mathf.Min(0.2f, remaining);
                            // Try a few finer steps
                            for (int s = 0; s < 6 && step > 0.001f; s++)
                            {
                                Vector3 stepMotion = motion.normalized * step;
                                Vector3 testTop = (rb.position + stepMotion) + Vector3.up * (capHeight / 2f);
                                Vector3 testBottom = (rb.position + stepMotion) - Vector3.up * (capHeight / 2f);

                                Collider[] overlaps = Physics.OverlapCapsule(testTop, testBottom, capRadius, collisionLayers, QueryTriggerInteraction.Ignore);
                                if (!TryGetBlockingOverlap(overlaps, out Collider blocking2))
                                {
                                    transform.position += stepMotion;
                                    moved = true;
                                    break;
                                }
                                else
                                {
                                    Debug.Log($"[EnemyKnockback] incremental fallback step blocked by '{blocking2?.name}'", this);
                                }

                                step *= 0.5f;
                            }

                            if (!moved)
                            {
                                Debug.Log($"[EnemyKnockback] Knockback blocked by capsule cast on '{name}' — hit '{hit.collider?.name}'. Stopping knockback.", this);
                                break;
                            }
                        }
                        else
                        {
                            Debug.Log($"[EnemyKnockback] Knockback blocked by capsule cast on '{name}' — hit '{hit.collider?.name}'. Stopping knockback.", this);
                            break;
                        }
                    }
                }



                // Hit wall/object and could not move
                if (Vector3.Distance(
                    oldPosition,
                    rb.position) < 0.001f &&
                    !rb.isKinematic) // note: for kinematic we moved transform; check transform delta below instead
                {
                    Debug.Log($"[EnemyKnockback] Knockback movement stalled on '{name}' (oldPos==newPos) — breaking.", this);
                    break;
                }
                else
                {
                    Vector3 newPos = rb.isKinematic ? transform.position : rb.position;
                    Debug.Log($"[EnemyKnockback] '{name}' moved from {oldPosition} to {newPos} during knockback.", this);
                }
            }



            elapsed += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }





        if (hadAgent && agent != null)
        {
            agent.Warp(rb.position);

            agent.updatePosition = true;

            agent.isStopped = false;
        }

        // re-enable movement
        if (movementComp != null)
            movementComp.SetMovementEnabled(true);

        Vector3 finalPos = rb.isKinematic ? transform.position : rb.position;
        Debug.Log($"[EnemyKnockback] KnockbackRoutine END on '{name}' finalPos={finalPos}", this);

        IsBeingKnockedBack = false;

        knockbackCoroutine = null;
    }

    // Return true and the first blocking collider if any. Ignores:
    //  - null entries
    //  - trigger colliders
    //  - colliders whose attachedRigidbody == this.rb (same body)
    //  - colliders whose root == this.transform.root (same multi-part prefab)
    //  - colliders that are children/parents of this transform
    //  - colliders on the Terrain layer (useful ground layer)
    private bool TryGetBlockingOverlap(Collider[] overlaps, out Collider blocking)
    {
        blocking = null;
        if (overlaps == null || overlaps.Length == 0) return false;

        int terrainLayer = LayerMask.NameToLayer("Terrain");

        foreach (var col in overlaps)
        {
            if (col == null) continue;

            // ignore triggers (they shouldn't block physical movement)
            if (col.isTrigger) continue;

            // ignore colliders on this exact transform
            if (col.transform == transform) continue;

            // ignore colliders on children of this object
            if (col.transform.IsChildOf(transform)) continue;

            // ignore colliders on parents/ancestors of this object
            if (transform.IsChildOf(col.transform)) continue;

            // ignore colliders that are part of the same Rigidbody (same physics body)
            if (col.attachedRigidbody == rb) continue;

            // ignore colliders that share the same top-level root (same prefab/actor)
            if (col.transform.root == transform.root) continue;

            // ignore terrain layer if present
            if (terrainLayer != -1 && col.gameObject.layer == terrainLayer) continue;

            // This collider belongs to another object -> blocking
            blocking = col;
            return true;
        }

        return false;
    }
}