using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpikeTrapTrigger: Place on a GameObject with a trigger Collider. When a valid target
/// (Player, Enemy, or Both) enters the trigger, all linked SpikeTrapMovement components
/// are activated. When all valid targets leave, the linked traps are deactivated.
///
/// Drag any spike trap GameObjects (that have SpikeTrapMovement) into the linkedTraps
/// array in the inspector to connect them.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SpikeTrapTrigger : MonoBehaviour
{
    public enum TargetMode
    {
        Player,
        Enemy,
        Both
    }

    [Header("Targets")]
    [Tooltip("Choose which targets activate the linked spike traps.")]
    [SerializeField] private TargetMode targetMode = TargetMode.Player;

    [Header("Linked Traps")]
    [Tooltip("Drag any spike trap GameObjects here. Their SpikeTrapMovement components will be toggled.")]
    [SerializeField] private SpikeTrapMovement[] linkedTraps;

    [Header("Behaviour")]
    [Tooltip("If true, linked traps start disabled and only move when a target is inside the trigger.")]
    [SerializeField] private bool disableTrapsOnStart = true;

    [Tooltip("If true, traps stay active permanently after first activation (one-shot trigger).")]
    [SerializeField] private bool stayActiveOnce = false;

    [Header("Damage")]
    [Tooltip("Damage dealt to valid targets on entering the trigger.")]
    [SerializeField] private float damageAmount = 20f;

    [Header("Knockback (Player only)")]
    [Tooltip("Distance the player is knocked back when hit.")]
    [SerializeField] private float knockbackDistance = 8f;

    private readonly HashSet<GameObject> _occupants = new HashSet<GameObject>();
    private bool _permanentlyActivated;

    private void Start()
    {
        // Ensure the collider on this object is a trigger
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[{nameof(SpikeTrapTrigger)}] Collider on {name} is not marked as isTrigger. Setting it now.");
            col.isTrigger = true;
        }

        if (disableTrapsOnStart)
            SetTrapsActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidTarget(other)) return;

        var root = other.transform.root.gameObject;
        bool wasEmpty = _occupants.Count == 0;
        _occupants.Add(root);

        // Activate linked traps when first valid target enters
        if (wasEmpty && !_permanentlyActivated)
        {
            SetTrapsActive(true);

            if (stayActiveOnce)
                _permanentlyActivated = true;
        }

        // Apply damage to the target that entered
        ApplyDamage(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_permanentlyActivated) return;

        var root = other.transform.root.gameObject;
        _occupants.Remove(root);

        // Deactivate traps when all valid targets have left
        if (_occupants.Count == 0)
            SetTrapsActive(false);
    }

    private bool IsValidTarget(Collider col)
    {
        // Player
        if ((targetMode == TargetMode.Player || targetMode == TargetMode.Both)
            && col.gameObject.CompareTag("Player"))
            return true;

        // Enemy
        if (targetMode == TargetMode.Enemy || targetMode == TargetMode.Both)
        {
            if (col.gameObject.CompareTag("Enemy")) return true;
            if (col.GetComponentInParent<EnemyBase>() != null) return true;
        }

        return false;
    }

    private void ApplyDamage(Collider other)
    {
        // --- Player damage via PlayerTakeDamage.TryApplyDamageAndKnockback ---
        if ((targetMode == TargetMode.Player || targetMode == TargetMode.Both)
            && other.gameObject.CompareTag("Player"))
        {
            // Knockback direction: away from trap center
            Vector3 dir = (other.transform.position - transform.position).normalized;
            dir.y = Mathf.Max(dir.y, 0.2f);

            // Prefer PlayerTakeDamage (handles damage, knockback, i-frames, flash)
            var playerTake = other.GetComponentInParent<PlayerTakeDamage>() ?? other.GetComponent<PlayerTakeDamage>();
            if (playerTake != null)
            {
                playerTake.TryApplyDamageAndKnockback(damageAmount, dir, knockbackDistance);
                return;
            }

            // Fallback: apply damage directly via Health
            var health = other.GetComponentInParent<Health>() ?? other.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDmg(damageAmount);
            }
            else
            {
                Debug.LogWarning($"[{nameof(SpikeTrapTrigger)}] Player has no PlayerTakeDamage or Health component.");
            }

            return;
        }

        // --- Enemy damage via IDamageable / EnemyBase / Health ---
        if (targetMode == TargetMode.Enemy || targetMode == TargetMode.Both)
        {
            // Prefer EnemyBase → IDamageable
            var enemyBase = other.GetComponent<EnemyBase>() ?? other.GetComponentInParent<EnemyBase>();
            if (enemyBase != null)
            {
                if (enemyBase is IDamageable dmgable)
                {
                    dmgable.TakeDmg(damageAmount);
                    return;
                }

                var enemyHealth = enemyBase.GetComponent<EnemyHealth>() ?? enemyBase.GetComponentInChildren<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damageAmount);
                    return;
                }
            }

            // Generic IDamageable on the collider or parents
            if (other.TryGetComponent<IDamageable>(out var genericDmg))
            {
                genericDmg.TakeDmg(damageAmount);
                return;
            }

            // Last resort: any Health component
            var fallbackHealth = other.GetComponentInParent<Health>() ?? other.GetComponent<Health>();
            if (fallbackHealth != null)
            {
                fallbackHealth.TakeDmg(damageAmount);
                return;
            }

            Debug.LogWarning($"[{nameof(SpikeTrapTrigger)}] Enemy {other.name} has no damageable component.");
        }
    }

    private void SetTrapsActive(bool active)
    {
        if (linkedTraps == null) return;

        foreach (var trap in linkedTraps)
        {
            if (trap != null)
                trap.enabled = active;
        }
    }

    private void OnDisable()
    {
        _occupants.Clear();
    }

    // Editor gizmo: draw lines from trigger to each linked trap
    private void OnDrawGizmosSelected()
    {
        if (linkedTraps == null) return;

        Gizmos.color = Color.red;
        foreach (var trap in linkedTraps)
        {
            if (trap != null)
                Gizmos.DrawLine(transform.position, trap.transform.position);
        }
    }
}
