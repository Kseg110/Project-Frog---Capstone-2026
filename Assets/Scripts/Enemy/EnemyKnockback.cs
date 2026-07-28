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



    [Header("Collision")]
    [Tooltip("Layers the enemy is blocked by while being knocked back.")]
    [SerializeField] private LayerMask collisionLayers;


    [Tooltip("Capsule used for collision-safe movement.")]
    [SerializeField] private CapsuleCollider capsule;



    public bool IsBeingKnockedBack { get; private set; }



    private Rigidbody rb;
    private NavMeshAgent agent;
    private Coroutine knockbackCoroutine;



    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        agent = GetComponent<NavMeshAgent>();


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


        float finalDistance =
            distance *
            knockbackResistance;


        if (finalDistance <= 0f)
            return;



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



        bool hadAgent =
            agent != null &&
            agent.enabled;



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
                    CollisionUtility.MoveWithCapsuleCollision(
                        rb,
                        capsule,
                        motion,
                        collisionLayers
                    );
                }
                else
                {
                    // Backup collision check
                    if (!Physics.CapsuleCast(
                        rb.position,
                        rb.position + Vector3.up,
                        0.3f,
                        motion.normalized,
                        out RaycastHit hit,
                        motion.magnitude,
                        collisionLayers,
                        QueryTriggerInteraction.Ignore))
                    {
                        rb.MovePosition(
                            rb.position + motion
                        );
                    }
                    else
                    {
                        break;
                    }
                }



                // Hit wall/object and could not move
                if (Vector3.Distance(
                    oldPosition,
                    rb.position) < 0.001f)
                {
                    break;
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



        IsBeingKnockedBack = false;

        knockbackCoroutine = null;
    }
}