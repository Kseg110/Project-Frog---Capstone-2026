using UnityEngine;
using FMODUnity;

/// <summary>
/// SpikeTrap: Attach to a trap parent object. Finds a child GameObject tagged
/// (default) "trap" that should contain an isTrigger Collider, and forwards its
/// trigger events here. When a valid target enters, damage + knockback are applied
/// through the canonical systems (PlayerTakeDamage for the player, EnemyBase for enemies).
/// This script does NOT implement its own health or knockback — it only detects and delegates.
/// </summary>
public class SpikeTrap : MonoBehaviour
{
    public enum TargetMode { Player, Enemy, Both }

    [Header("Damage")]
    [SerializeField] private float damageAmount = 20f;

    [Header("Knockback")]
    [Tooltip("Knockback distance passed to the target's knockback system.")]
    [SerializeField] private float knockbackDistance = 8f;

    [Header("Targets")]
    [SerializeField] private TargetMode targetMode = TargetMode.Player;

    [Header("Trigger")]
    [Tooltip("Child object tag to use as the trigger that activates this spike trap.")]
    [SerializeField] private string triggerTag = "trap";

    [Header("FMod Events")]
    [SerializeField] private EventReference trapActivateEvent;

    private GameObject triggerChild;

    private void Start()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject != gameObject && t.gameObject.CompareTag(triggerTag))
            {
                triggerChild = t.gameObject;
                break;
            }
        }

        //if (triggerChild == null)
        //{
        //    Debug.LogWarning($"[{nameof(SpikeTrap)}] No child with tag \"{triggerTag}\" found under {name}.");
        //    return;
        //}

        var col = triggerChild.GetComponent<Collider>();
        if (col == null)
            Debug.LogWarning($"[{nameof(SpikeTrap)}] Child tagged \"{triggerTag}\" on {triggerChild.name} has no Collider.");
        else if (!col.isTrigger)
            Debug.LogWarning($"[{nameof(SpikeTrap)}] Collider on {triggerChild.name} is not marked as isTrigger.");

        var forwarder = triggerChild.GetComponent<SpikeTrapTriggerForwarder>()
                        ?? triggerChild.AddComponent<SpikeTrapTriggerForwarder>();
        forwarder.parent = this;
    }

    // Called by the forwarder when something enters the child trigger.
    internal void OnChildTriggerEnter(Collider other)
    {
        bool hitSomething = false;

        if (targetMode == TargetMode.Player || targetMode == TargetMode.Both)
        {
            var playerTake = other.GetComponentInParent<PlayerTakeDamage>();
            if (playerTake != null)
            {
                Vector3 dir = KnockDir(other);
                // Canonical player pipeline: shield, i-frames, Health.TakeDmg, knockback, flash.
                playerTake.TryApplyDamageAndKnockback(damageAmount, dir, knockbackDistance);
                hitSomething = true;
            }
        }

        if (!hitSomething && (targetMode == TargetMode.Enemy || targetMode == TargetMode.Both))
        {
            var enemyBase = other.GetComponentInParent<EnemyBase>();
            if (enemyBase != null)
            {
                if (enemyBase is IDamageable dmgable)
                    dmgable.TakeDmg(damageAmount);
                else
                    Debug.LogWarning($"[{nameof(SpikeTrap)}] {enemyBase.name} is not IDamageable.");

                var enemyKnock = enemyBase.GetComponent<EnemyKnockback>();
                if (enemyKnock != null)
                    enemyKnock.ApplyKnockback(KnockDir(other), knockbackDistance);

                hitSomething = true;
            }
        }

        if (hitSomething)
            RuntimeManager.PlayOneShot(trapActivateEvent, transform.position);
    }

    // Knockback direction: away from the trap, with a little upward lift.
    private Vector3 KnockDir(Collider other)
    {
        Vector3 dir = (other.transform.position - transform.position).normalized;
        dir.y = Mathf.Max(dir.y, 0.2f);
        return dir.normalized;
    }
}

/// <summary>
/// Lightweight forwarder placed on the child trigger object; relays trigger events to the parent SpikeTrap.
/// </summary>
public class SpikeTrapTriggerForwarder : MonoBehaviour
{
    [HideInInspector] public SpikeTrap parent;

    private void OnTriggerEnter(Collider other) => parent?.OnChildTriggerEnter(other);
}