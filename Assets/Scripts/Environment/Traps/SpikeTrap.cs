using UnityEngine;

/// <summary>
/// SpikeTrap: Attach to a trap parent object. The script finds a child GameObject tagged
/// (default) "trap damage" that should contain an isTrigger Collider and forwards trigger
/// events to this component. When a Player enters the trigger the player will be damaged
/// and knocked back.
/// </summary>
public class SpikeTrap : MonoBehaviour
{
    public enum TargetMode
    {
        Player,
        Enemy,
        Both
    }

    [Header("Damage")]
    [SerializeField] private float damageAmount = 20f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float knockbackDuration = 0.25f;

    [Header("Targets")]
    [Tooltip("Choose whether the trap hurts the Player, Enemies, or Both.")]
    [SerializeField] private TargetMode targetMode = TargetMode.Player;

    [Header("Trigger")]
    [Tooltip("Child object tag to use as the trigger that activates this spike trap.")]
    [SerializeField] private string triggerTag = "trap";

    private GameObject triggerChild;

    private void Start()
    {
        // Find first child tagged as the trap trigger (e.g. "trap damage")
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject != gameObject && t.gameObject.CompareTag(triggerTag))
            {
                triggerChild = t.gameObject;
                break;
            }
        }

        if (triggerChild == null)
        {
            Debug.LogWarning($"[{nameof(SpikeTrap)}] No child with tag \"{triggerTag}\" found under {name}.");
            return;
        }

        var col = triggerChild.GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"[{nameof(SpikeTrap)}] Child tagged \"{triggerTag}\" on {triggerChild.name} has no Collider.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"[{nameof(SpikeTrap)}] Collider on {triggerChild.name} is not marked as isTrigger. Mark it as trigger for trap activation.");
        }

        // Add or get forwarding component so the child's trigger events are forwarded here
        var forwarder = triggerChild.GetComponent<SpikeTrapTriggerForwarder>() ?? triggerChild.AddComponent<SpikeTrapTriggerForwarder>();
        forwarder.parent = this;
    }

    // Called by the trigger forwarder when something enters the child trigger
    internal void OnChildTriggerEnter(Collider other)
    {
        // If trap should damage player (or both), check for player
        if (targetMode == TargetMode.Player || targetMode == TargetMode.Both)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                HandlePlayerHit(other);
            }
        }

        // If trap should damage enemies (or both), check for enemy
        if (targetMode == TargetMode.Enemy || targetMode == TargetMode.Both)
        {
            // Prefer EnemyBase component, otherwise try IDamageable or EnemyHealth, or tag "Enemy"
            if (other.TryGetComponent<EnemyBase>(out var enemyBase))
            {
                HandleEnemyHit(other, enemyBase);
            }
            else
            {
                // Try parent objects as enemies are often on parents
                var parentEnemyBase = other.GetComponentInParent<EnemyBase>();
                if (parentEnemyBase != null)
                {
                    HandleEnemyHit(other, parentEnemyBase);
                }
                else
                {
                    // fallback: if object has IDamageable or EnemyHealth, treat as enemy
                    if (other.TryGetComponent<IDamageable>(out var dmgable))
                    {
                        ApplyDamageToIDamageable(dmgable);
                        ApplyKnockbackToCollider(other);
                    }
                    else
                    {
                        var enemyHealth = other.GetComponentInParent<EnemyHealth>();
                        if (enemyHealth != null)
                        {
                            enemyHealth.TakeDamage(damageAmount);
                            ApplyKnockbackToCollider(other);
                        }
                        else if (other.gameObject.CompareTag("Enemy"))
                        {
                            // Last resort: try to damage via any Health component on the enemy root
                            var fallback = other.GetComponentInParent<Health>();
                            if (fallback != null)
                            {
                                fallback.TakeDmg(damageAmount);
                                ApplyKnockbackToCollider(other);
                            }
                        }
                    }
                }
            }
        }
    }

    // Reattached: damage the player via PlayerMovement root instead of TopDownControllerWithDash.
    private void HandlePlayerHit(Collider other)
    {
        // Prefer PlayerTakeDamage (which encapsulates TryApplyDamageAndKnockback)
        var playerTake = other.GetComponentInParent<PlayerTakeDamage>() ?? other.GetComponent<PlayerTakeDamage>();
        Vector3 dir = (other.transform.position - transform.position).normalized;
        dir.y = Mathf.Max(dir.y, 0.2f); // give a little upward lift

        if (playerTake != null)
        {
            // Use the PlayerTakeDamage API to apply damage + knockback (distance argument uses knockbackForce)
            playerTake.TryApplyDamageAndKnockback(damageAmount, dir, knockbackForce);
            return;
        }

        // Fallback: if PlayerTakeDamage isn't present, try to find PlayerMovement + Health and apply manually
        var playerMovement = other.GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogWarning($"[{nameof(SpikeTrap)}] Player does not have a PlayerMovement component.");
            return;
        }

        // Try to find a Health component on the same root as PlayerMovement first
        var health = playerMovement.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (health != null)
        {
            health.TakeDmg(damageAmount);
        }
        else
        {
            Debug.LogWarning($"[{nameof(SpikeTrap)}] Player has no Health component to take damage.");
        }

        // Compute knockback distance and direction
        Vector3 knockback = dir * knockbackForce;

        // Apply knockback: prefer Rigidbody on player root. If kinematic, move it directly.
        var rb = playerMovement.GetComponent<Rigidbody>() ?? other.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            if (rb.isKinematic)
            {
                // Move the kinematic rigidbody by a single displacement to simulate knockback.
                rb.MovePosition(rb.position + knockback);
            }
            else
            {
                rb.AddForce(knockback, ForceMode.Impulse);
            }
        }
        else
        {
            // Fallback: nudge root transform (last resort)
            other.transform.root.position += knockback;
        }
    }

    private void HandleEnemyHit(Collider other, EnemyBase enemyBase)
    {
        // Try to apply damage via IDamageable if available
        if (enemyBase is IDamageable dmgable)
        {
            dmgable.TakeDmg(damageAmount);
        }
        else
        {
            // Fallback to EnemyHealth if present
            var enemyHealth = enemyBase.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
                enemyHealth.TakeDamage(damageAmount);
            else
            {
                // Last resort: try any Health on enemy root
                var fallback = enemyBase.GetComponentInParent<Health>();
                if (fallback != null)
                    fallback.TakeDmg(damageAmount);
            }
        }

        ApplyKnockbackToCollider(other);
    }

    private void ApplyDamageToIDamageable(IDamageable dmgable)
    {
        dmgable.TakeDmg(damageAmount);
    }

    // Updated knockback: if target is a player, route through PlayerTakeDamage.TryApplyDamageAndKnockback
    private void ApplyKnockbackToCollider(Collider other)
    {
        // Compute knockback direction (away from trap center)
        Vector3 dir = (other.transform.position - transform.position).normalized;
        dir.y = Mathf.Max(dir.y, 0.1f);
        float distance = knockbackForce; // distance parameter expected by PlayerTakeDamage

        // If this is a player, prefer PlayerTakeDamage API
        var playerTake = other.GetComponentInParent<PlayerTakeDamage>() ?? other.GetComponent<PlayerTakeDamage>();
        if (playerTake != null)
        {
            // Damage amount already applied by caller in most flows; TryApplyDamageAndKnockback enforces i-frames,
            // so we call it to apply damage & knockback only if appropriate.
            playerTake.TryApplyDamageAndKnockback(damageAmount, dir, distance);
            return;
        }

        // Otherwise try Rigidbody on the object's root
        var rb = other.GetComponentInParent<Rigidbody>() ?? other.GetComponent<Rigidbody>() ?? other.GetComponentInChildren<Rigidbody>();
        Vector3 knockback = dir * knockbackForce;
        if (rb != null)
        {
            if (rb.isKinematic)
            {
                rb.MovePosition(rb.position + knockback);
            }
            else
            {
                rb.AddForce(knockback, ForceMode.Impulse);
            }
        }
        else
        {
            // Fallback: nudge root transform
            other.transform.root.position += knockback;
        }
    }

    private void OnDisable()
    {
        // no-op; ensure any coroutines in player are unaffected
    }
}

/// <summary>
/// Lightweight forwarder put on the child trigger object that calls back into the parent SpikeTrap.
/// </summary>
public class SpikeTrapTriggerForwarder : MonoBehaviour
{
    [HideInInspector] public SpikeTrap parent;

    private void OnTriggerEnter(Collider other)
    {
        parent?.OnChildTriggerEnter(other);
    }
}
