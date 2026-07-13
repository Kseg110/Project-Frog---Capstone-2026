using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PoisonBomb: a triggered bomb that, when armed by a trigger child sphere and the player passes through it,
/// explodes and applies a damage-over-time (DOT) "poison" status to Player, Enemy, or Both within an explosion radius.
/// The DOT persists on affected targets for `dotDuration` even if they leave the explosion radius.
/// 
/// Usage:
/// - Add this to the bomb GameObject. Create a child Sphere (or leave blank and the script will create one)
///   that acts as the arming trigger. The child will receive a forwarder so the parent is notified on trigger enter.
/// - Configure target mode, explosion radius, DOT values, layers, particle root, and whether the bomb is destroyed on explode.
/// </summary>
public class PoisonBomb : MonoBehaviour
{
    public enum TargetMode
    {
        Player,
        Enemy,
        Both
    }

    [Header("Explosion")]
    [Tooltip("Radius of explosion that determines who gets poisoned.")]
    public float explosionRadius = 5f;

    [Tooltip("Layer mask used when checking for targets in the explosion radius.")]
    public LayerMask affectLayerMask = ~0;

    [Header("DOT (applied to each affected target)")]
    [Tooltip("Damage applied per tick.")]
    public float damagePerTick = 4f;

    [Tooltip("Seconds between damage ticks.")]
    public float tickInterval = 1f;

    [Tooltip("Total duration (seconds) of the DOT effect applied to each affected target.")]
    public float dotDuration = 6f;

    [Header("Targets")]
    [Tooltip("Which types of actors are affected by the explosion.")]
    public TargetMode targetMode = TargetMode.Player;

    [Header("Knockback (Optional)")]
    [Tooltip("If true, the explosion will knock back affected targets away from the bomb center.")]
    public bool applyKnockback = false;

    [Tooltip("Distance the player is knocked back (used by PlayerTakeDamage). For enemies this is used as force magnitude.")]
    public float knockbackDistance = 5f;

    [Header("Trigger child")]
    [Tooltip("Optional child transform to use as the arming trigger. If null, a child named 'TriggerSphere' will be created.")]
    public Transform triggerChild;

    [Tooltip("Radius of the arm trigger child. This is the radius that, when the player passes through, arms and detonates the bomb.")]
    public float triggerChildRadius = 1.5f;

    [Header("Particles / Visuals")]
    [Tooltip("Optional transform containing ParticleSystem(s) to play on explosion (will be played).")]
    public Transform particleRoot;

    [Tooltip("If true, the particle systems will be played on explosion.")]
    public bool playParticlesOnExplode = true;

    [Header("Destruction")]
    [Tooltip("Destroy the bomb GameObject after exploding.")]
    public bool destroyOnExplode = true;

    [Tooltip("Delay in seconds before destroying the bomb after explosion.")]
    public float destroyDelay = 0.1f;

    bool hasExploded = false;
    ParticleSystem[] particleSystems;

    void Start()
    {
        EnsureTriggerChild();
        CacheParticleSystems();
    }

    void EnsureTriggerChild()
    {
        if (triggerChild != null)
        {
            EnsureTriggerOnTransform(triggerChild);
            return;
        }

        // try to find an existing child SphereCollider tag or name first
        foreach (Transform t in transform)
        {
            var sc = t.GetComponent<SphereCollider>();
            if (sc != null)
            {
                triggerChild = t;
                EnsureTriggerOnTransform(triggerChild);
                return;
            }
        }

        // create the trigger child if none found
        var go = new GameObject("TriggerSphere");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        triggerChild = go.transform;
        EnsureTriggerOnTransform(triggerChild);
    }

    void EnsureTriggerOnTransform(Transform t)
    {
        var sc = t.GetComponent<SphereCollider>();
        if (sc == null) sc = t.gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = Mathf.Max(0.01f, triggerChildRadius);
        sc.center = Vector3.zero;

        // add forwarder so child trigger events call back to this PoisonBomb
        var forwarder = t.GetComponent<PoisonBombTriggerForwarder>();
        if (forwarder == null) forwarder = t.gameObject.AddComponent<PoisonBombTriggerForwarder>();
        forwarder.parent = this;
    }

    void CacheParticleSystems()
    {
        if (particleRoot != null)
            particleSystems = particleRoot.GetComponentsInChildren<ParticleSystem>(true);
        else
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    // Called by the trigger forwarder when something enters the child trigger
    internal void OnTriggerChildEnter(Collider other)
    {
        if (hasExploded) return;

        // Only arm/explode when the player passes through the trigger child as requested.
        if (!other.gameObject.CompareTag("Player")) return;

        Explode();
    }

    /// <summary>
    /// Finds the actual character root (the GameObject holding Health / PlayerTakeDamage / EnemyBase)
    /// instead of blindly using transform.root, which may point to a scene container.
    /// </summary>
    GameObject FindCharacterRoot(Collider hit)
    {
        var ptd = hit.GetComponentInParent<PlayerTakeDamage>();
        if (ptd != null) return ptd.gameObject;

        var eb = hit.GetComponentInParent<EnemyBase>();
        if (eb != null) return eb.gameObject;

        var h = hit.GetComponentInParent<Health>();
        if (h != null) return h.gameObject;

        return hit.transform.root.gameObject;
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // Play particle effects
        if (playParticlesOnExplode && particleSystems != null)
        {
            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;
                ps.gameObject.SetActive(true);
                ps.Play(true);
            }
        }

        // FIX: Use QueryTriggerInteraction.Collide so trigger colliders (e.g. kinematic player) are found
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, affectLayerMask, QueryTriggerInteraction.Collide);

        // Keep a set of roots we will apply DOT to (so each GameObject only receives the status once)
        HashSet<GameObject> targets = new HashSet<GameObject>();

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            if (!IsValidTarget(hit)) continue;

            // FIX: resolve the actual character root via components, not transform.root
            GameObject root = FindCharacterRoot(hit);
            if (targets.Contains(root)) continue;

            targets.Add(root);

            // Apply knockback if enabled (do it per-target using the actual collider for direction)
            if (applyKnockback)
            {
                ApplyKnockbackToTarget(hit);
            }
        }

        // Apply poison status to each collected target (status persists even if they leave radius)
        foreach (var tgt in targets)
        {
            if (tgt == null) continue;

            // attach or refresh PoisonedStatus component on the target root
            var status = tgt.GetComponent<PoisonedStatus>();
            if (status == null)
            {
                status = tgt.AddComponent<PoisonedStatus>();
                status.Configure(damagePerTick, tickInterval, dotDuration);
            }
            else
            {
                status.Refresh(damagePerTick, tickInterval, dotDuration);
            }
        }

        if (destroyOnExplode)
        {
            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        }
    }

    void ApplyKnockbackToTarget(Collider hit)
    {
        // Knockback direction: away from bomb center
        Vector3 dir = (hit.transform.position - transform.position).normalized;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.up; // fallback if directly on top
        dir.y = Mathf.Max(dir.y, 0.1f);

        // --- Player: prefer PlayerTakeDamage.TryApplyDamageAndKnockback for i-frames + knockback ---
        var playerTake = hit.GetComponentInParent<PlayerTakeDamage>();
        if (playerTake != null)
        {
            // Pass 0 damage here because DOT is applied separately via PoisonedStatus;
            // this call is purely for the knockback + i-frame flash effect.
            playerTake.TryApplyDamageAndKnockback(0f, dir, knockbackDistance);
            return;
        }

        // --- Enemy or fallback: use Rigidbody if available ---
        var rb = hit.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            if (rb.isKinematic)
                rb.MovePosition(rb.position + dir * knockbackDistance);
            else
                rb.AddForce(dir * knockbackDistance, ForceMode.Impulse);
        }
        else
        {
            hit.transform.root.position += dir * knockbackDistance;
        }
    }

    bool IsValidTarget(Collider hit)
    {
        if (hit == null) return false;

        // FIX: check the collider itself AND parents for the Player tag / components
        bool isPlayer = hit.gameObject.CompareTag("Player")
                     || hit.GetComponentInParent<PlayerTakeDamage>() != null;

        bool isEnemy = hit.gameObject.CompareTag("Enemy")
                    || hit.GetComponentInParent<EnemyBase>() != null;

        // Player
        if ((targetMode == TargetMode.Player || targetMode == TargetMode.Both) && isPlayer)
            return true;

        // Enemy
        if ((targetMode == TargetMode.Enemy || targetMode == TargetMode.Both) && isEnemy)
            return true;

        // Additional fallback: any non-player with a Health component counts as enemy
        if ((targetMode == TargetMode.Enemy || targetMode == TargetMode.Both))
        {
            if (hit.GetComponentInParent<Health>() != null && !isPlayer)
                return true;
        }

        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.12f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        if (triggerChild != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.12f);
            Gizmos.DrawSphere(triggerChild.position, triggerChildRadius);
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 1f);
            Gizmos.DrawWireSphere(triggerChild.position, triggerChildRadius);
        }
    }
}

/// <summary>
/// Status component added to affected targets to apply DOT independent of bomb radius.
/// It self-destructs when duration expires. If already present, Refresh will extend/reconfigure it.
/// </summary>
public class PoisonedStatus : MonoBehaviour
{
    float damagePerTick;
    float tickInterval;
    float remainingDuration;
    Coroutine tickCoroutine;

    public void Configure(float damagePerTick, float tickInterval, float duration)
    {
        this.damagePerTick = damagePerTick;
        this.tickInterval = Mathf.Max(0.05f, tickInterval);
        this.remainingDuration = Mathf.Max(0f, duration);

        if (tickCoroutine != null) StopCoroutine(tickCoroutine);
        tickCoroutine = StartCoroutine(DoDOT());
    }

    // Update values and extend duration
    public void Refresh(float damagePerTick, float tickInterval, float duration)
    {
        this.damagePerTick = damagePerTick;
        this.tickInterval = Mathf.Max(0.05f, tickInterval);
        // If refreshed with a longer duration, extend remaining duration to the new value
        remainingDuration = Mathf.Max(remainingDuration, duration);

        if (tickCoroutine == null)
            tickCoroutine = StartCoroutine(DoDOT());
    }

    IEnumerator DoDOT()
    {
        float nextTick = 0f;
        while (remainingDuration > 0f)
        {
            float dt = Mathf.Min(tickInterval, remainingDuration);
            // Wait until next tick time (on first iteration dt may be 0 to apply immediate damage)
            if (nextTick > 0f)
                yield return new WaitForSeconds(nextTick);

            ApplyDamage();
            remainingDuration -= tickInterval;
            nextTick = tickInterval;
        }

        // done
        tickCoroutine = null;
        Destroy(this); // remove status component
        yield break;
    }

    void ApplyDamage()
    {
        if (gameObject == null) return;

        // Prefer IDamageable on this object or children
        if (TryGetComponent<IDamageable>(out var dmgable))
        {
            dmgable.TakeDmg(damagePerTick);
            return;
        }

        // EnemyBase (may implement IDamageable)
        var enemyBase = GetComponentInChildren<EnemyBase>() ?? GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            if (enemyBase is IDamageable d) { d.TakeDmg(damagePerTick); return; }

            var eh = enemyBase.GetComponent<EnemyHealth>() ?? enemyBase.GetComponentInChildren<EnemyHealth>();
            if (eh != null) { eh.TakeDamage(damagePerTick); return; }

            var fallback = enemyBase.GetComponent<Health>() ?? enemyBase.GetComponentInChildren<Health>();
            if (fallback != null) { fallback.TakeDmg(damagePerTick); return; }
        }

        // Player or generic health fallback (check self, children, then parents)
        var health = GetComponent<Health>() ?? GetComponentInChildren<Health>() ?? GetComponentInParent<Health>();
        if (health != null)
        {
            health.TakeDmg(damagePerTick);
            return;
        }
    }
}

/// <summary>
/// Light forwarder attachable to the trigger child. Forwards OnTriggerEnter to parent PoisonBomb.
/// </summary>
public class PoisonBombTriggerForwarder : MonoBehaviour
{
    [HideInInspector] public PoisonBomb parent;

    void OnTriggerEnter(Collider other)
    {
        parent?.OnTriggerChildEnter(other);
    }
}
