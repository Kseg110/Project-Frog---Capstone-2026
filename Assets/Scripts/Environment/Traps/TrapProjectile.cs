using UnityEngine;

//attach to projectile prefab spawned by trap when charged, handles movement and damage on hit

[RequireComponent(typeof(Collider))]
public class TrapProjectile : Projectile
{
    public enum TargetMode
    { Player, Enemy, Both }

    [Header("Collision")]
    [Tooltip("Choose whether this projectile damages Player, Enemy, or Both.")]
    [SerializeField] private TargetMode targetMode = TargetMode.Enemy;
    [SerializeField] private bool destroyOnHit = true;

    [Header("Knockback")]
    [Tooltip("Distance used when applying knockback from this trap projectile.")]
    [SerializeField] private float projectileKnockbackDistance = 2f;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col == null)
            col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider other)
    {
        if (other == null) return;

        bool dealtDamage = false;

        // PLAYER: prefer component lookup so child hitboxes work even if tag isn't directly on the collider
        if (targetMode == TargetMode.Player || targetMode == TargetMode.Both)
        {
            var playerMovement = other.GetComponentInParent<PlayerMovement>();
            bool isPlayer = playerMovement != null || other.gameObject.CompareTag("Player");
            if (isPlayer)
            {
                Health health = null;
                if (playerMovement != null)
                    health = playerMovement.GetComponent<Health>() ?? playerMovement.GetComponentInChildren<Health>();
                if (health == null)
                    health = other.GetComponentInParent<Health>();

                if (health != null)
                {
                    health.TakeDmg(damage);
                    dealtDamage = true;
                }
                else
                {
                    Debug.LogWarning($"[{nameof(TrapProjectile)}] Hit Player but no Health component found on {other.name}.");
                    dealtDamage = true; // consider it handled to avoid hitting enemy branch when same object is tagged differently
                }

                // Apply player knockback via PlayerTakeDamage if available
                var playerTake = other.GetComponentInParent<PlayerTakeDamage>() ?? other.GetComponent<PlayerTakeDamage>();
                if (playerTake != null)
                {
                    Vector3 dir = (other.transform.position - transform.position).normalized;
                    dir.y = 0f;
                    playerTake.TryApplyDamageAndKnockback(0f, dir, projectileKnockbackDistance);
                }
                else
                {
                    // fallback: use rigidbody/transform nudge for player if PlayerTakeDamage not present
                    var rb = other.GetComponentInParent<Rigidbody>() ?? other.GetComponent<Rigidbody>();
                    Vector3 dir = (other.transform.position - transform.position).normalized;
                    if (rb != null)
                    {
                        if (rb.isKinematic)
                            rb.MovePosition(rb.position + dir * projectileKnockbackDistance);
                        else
                            rb.AddForce(dir * projectileKnockbackDistance, ForceMode.Impulse);
                    }
                    else
                    {
                        other.transform.root.position += dir * projectileKnockbackDistance;
                    }
                }
            }
        }

        // ENEMY
        if ((targetMode == TargetMode.Enemy || targetMode == TargetMode.Both) && !dealtDamage)
        {
            // Prefer EnemyBase, then IDamageable, EnemyHealth, then tag fallback
            if (other.TryGetComponent<EnemyBase>(out var enemyBase))
            {
                if (enemyBase is IDamageable dmgable)
                {
                    dmgable.TakeDmg(damage);
                }
                else
                {
                    var enemyHealth = enemyBase.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                        enemyHealth.TakeDamage(damage);
                    else
                    {
                        var fallback = enemyBase.GetComponentInParent<Health>();
                        if (fallback != null)
                            fallback.TakeDmg(damage);
                        else
                            Debug.LogWarning($"[{nameof(TrapProjectile)}] Hit Enemy but no damageable component found on {other.name}.");
                    }
                }

                // Apply enemy knockback: prefer EnemyKnockback component
                Vector3 pushDir = (enemyBase.transform.position - transform.position);
                pushDir.y = 0f;
                if (pushDir.sqrMagnitude > 0.0001f)
                {
                    pushDir.Normalize();
                    var ek = enemyBase.GetComponentInParent<EnemyKnockback>();
                    if (ek != null)
                    {
                        ek.ApplyKnockback(pushDir, projectileKnockbackDistance);
                    }
                    else
                    {
                        var rb = other.attachedRigidbody ?? enemyBase.GetComponentInParent<Rigidbody>();
                        if (rb != null)
                        {
                            if (rb.isKinematic)
                                rb.MovePosition(rb.position + pushDir * projectileKnockbackDistance);
                            else
                                rb.AddForce(pushDir * projectileKnockbackDistance, ForceMode.Impulse);
                        }
                        else
                        {
                            enemyBase.transform.root.position += pushDir * projectileKnockbackDistance;
                        }
                    }
                }

                dealtDamage = true;
            }
            else
            {
                var parentEnemy = other.GetComponentInParent<EnemyBase>();
                if (parentEnemy != null)
                {
                    if (parentEnemy is IDamageable pdmg)
                        pdmg.TakeDmg(damage);
                    else
                    {
                        var eh = parentEnemy.GetComponent<EnemyHealth>();
                        if (eh != null) eh.TakeDamage(damage);
                        else
                        {
                            var fallback = parentEnemy.GetComponentInParent<Health>();
                            if (fallback != null) fallback.TakeDmg(damage);
                            else Debug.LogWarning($"[{nameof(TrapProjectile)}] Hit Enemy but no damageable component found on {other.name}.");
                        }
                    }

                    // Apply knockback for parent enemy
                    Vector3 pushDir = (parentEnemy.transform.position - transform.position);
                    pushDir.y = 0f;
                    if (pushDir.sqrMagnitude > 0.0001f)
                    {
                        pushDir.Normalize();
                        var ek = parentEnemy.GetComponentInParent<EnemyKnockback>();
                        if (ek != null)
                            ek.ApplyKnockback(pushDir, projectileKnockbackDistance);
                        else
                        {
                            var rb = other.attachedRigidbody ?? parentEnemy.GetComponentInParent<Rigidbody>();
                            if (rb != null)
                            {
                                if (rb.isKinematic)
                                    rb.MovePosition(rb.position + pushDir * projectileKnockbackDistance);
                                else
                                    rb.AddForce(pushDir * projectileKnockbackDistance, ForceMode.Impulse);
                            }
                            else
                            {
                                parentEnemy.transform.root.position += pushDir * projectileKnockbackDistance;
                            }
                        }
                    }

                    dealtDamage = true;
                }
                else
                {
                    if (other.TryGetComponent<IDamageable>(out var anyDmg))
                    {
                        anyDmg.TakeDmg(damage);
                        dealtDamage = true;
                    }
                    else if (other.gameObject.CompareTag("Enemy"))
                    {
                        var fallbackHealth = other.GetComponentInParent<Health>();
                        if (fallbackHealth != null)
                        {
                            fallbackHealth.TakeDmg(damage);
                            dealtDamage = true;
                        }
                    }
                }
            }
        }

        if (dealtDamage && destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
