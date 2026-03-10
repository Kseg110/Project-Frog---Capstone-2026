using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PoisionTrap: Deals damage-over-time to Player, Enemy, or Both inside a radius.
/// - Automatically ensures a trigger SphereCollider is present and sized to `radius`.
/// - Tracks occupants and applies damage every `tickInterval` seconds.
/// - Can toggle a child ParticleSystem on/off (assign `particleRoot` or it will find the first child ParticleSystem).
/// </summary>
public class PoisonTrap : MonoBehaviour
{
    public enum TargetMode
    {
        Player,
        Enemy,
        Both
    }

    [Header("DOT Settings")]
    [Tooltip("Damage applied per tick.")]
    public float damagePerTick = 5f;

    [Tooltip("Seconds between damage ticks.")]
    public float tickInterval = 1f;

    [Header("Area")]
    [Tooltip("Radius of effect (meters). A SphereCollider trigger will be created/adjusted to this size.")]
    public float radius = 3f;

    [Tooltip("Optional layer mask to restrict which colliders are considered (helps performance). Leave all layers to affect everything.")]
    public LayerMask affectLayer = ~0;

    [Header("Targets")]
    [Tooltip("Choose whether trap affects Player, Enemy or Both.")]
    public TargetMode targetMode = TargetMode.Player;

    [Header("Particles")]
    [Tooltip("Optional transform containing the ParticleSystem(s) to toggle when trap is active.")]
    public Transform particleRoot;

    [Tooltip("If true the particle system is enabled while trap is active.")]
    public bool enableParticlesWhenActive = true;

    [Header("Destruction")]
    [Tooltip("If true the trap will be destroyed when a valid target collides/enters the trap.")]
    public bool destroyOnCollision = false;

    [Tooltip("Delay in seconds before destroying the trap when triggered by collision/enter.")]
    public float destroyDelay = 0f;

    // Internal: track occupants and next time they should take damage
    readonly Dictionary<GameObject, float> occupantsNextTick = new Dictionary<GameObject, float>();

    SphereCollider triggerCollider;
    ParticleSystem[] particleSystems;

    // Preserve trivial Start/Update lifecycle comments as in original file.
    void Start()
    {
        EnsureTriggerCollider();
        CacheParticleSystems();

        // By default disable particles until trap is explicitly toggled on (if enabled in inspector).
        if (particleSystems != null && particleSystems.Length > 0)
            SetParticlesActive(false);
    }

    void Update()
    {
        // Apply periodic damage to tracked occupants.
        if (occupantsNextTick.Count == 0) return;

        float now = Time.time;
        var keys = new List<GameObject>(occupantsNextTick.Keys);
        foreach (var go in keys)
        {
            if (go == null)
            {
                occupantsNextTick.Remove(go);
                continue;
            }

            if (now >= occupantsNextTick[go])
            {
                ApplyDamageToTarget(go);
                occupantsNextTick[go] = now + tickInterval;
            }
        }
    }

    void OnDestroy()
    {
        occupantsNextTick.Clear();
    }

    // Ensure a SphereCollider exists and is configured as a trigger sized to 'radius'
    void EnsureTriggerCollider()
    {
        triggerCollider = GetComponent<SphereCollider>();
        if (triggerCollider == null)
            triggerCollider = gameObject.AddComponent<SphereCollider>();

        triggerCollider.isTrigger = true;
        triggerCollider.radius = Mathf.Max(0.01f, radius);
        // If object uses scaling, we keep the collider radius as-is; inspector radius is the local radius used.
    }

    void CacheParticleSystems()
    {
        if (particleRoot != null)
            particleSystems = particleRoot.GetComponentsInChildren<ParticleSystem>(true);
        else
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            // If parent has multiple particle systems and you want a specific child, assign particleRoot in inspector.
        }
    }

    // Public API to toggle the particle visual (and whether trap is considered "active" by visuals)
    public void SetParticlesActive(bool active)
    {
        if (particleSystems == null || particleSystems.Length == 0) return;
        foreach (var ps in particleSystems)
        {
            var go = ps.gameObject;
            if (active)
            {
                if (!go.activeSelf) go.SetActive(true);
                if (!ps.isPlaying) ps.Play(true);
            }
            else
            {
                if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                // keep the GameObject enabled so designer can toggle root; to fully hide call SetActive(false)
                go.SetActive(false);
            }
        }
    }

    // Called by Unity when something enters the trigger
    void OnTriggerEnter(Collider other)
    {
        if (!IsInAffectLayer(other.gameObject)) return;
        if (!IsValidTarget(other)) return;

        var root = other.transform.root.gameObject;
        if (!occupantsNextTick.ContainsKey(root))
        {
            // apply immediate damage on enter, then schedule next tick
            ApplyDamageToTarget(root);
            occupantsNextTick[root] = Time.time + tickInterval;
        }

        // Optionally enable particles when trap is active
        if (enableParticlesWhenActive && particleSystems != null && particleSystems.Length > 0)
            SetParticlesActive(true);

        // Optionally destroy trap on collision/enter
        if (destroyOnCollision)
        {
            // Schedule destruction after delay (0 = immediate end of frame)
            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        }
    }

    // Support non-trigger collisions as well (forward to trigger handling)
    void OnCollisionEnter(Collision collision)
    {
        // Forward to trigger-like handling for convenience. Collision.collider will be evaluated by same logic.
        OnTriggerEnter(collision.collider);
    }

    void OnTriggerExit(Collider other)
    {
        var root = other.transform.root.gameObject;
        if (occupantsNextTick.ContainsKey(root))
            occupantsNextTick.Remove(root);

        // If no occupants remain, optionally disable particles
        if (occupantsNextTick.Count == 0 && enableParticlesWhenActive && particleSystems != null && particleSystems.Length > 0)
            SetParticlesActive(false);
    }

    bool IsInAffectLayer(GameObject go)
    {
        int goLayerMask = 1 << go.layer;
        return (affectLayer & goLayerMask) != 0;
    }

    bool IsValidTarget(Collider col)
    {
        // Player
        if ((targetMode == TargetMode.Player || targetMode == TargetMode.Both) && col.gameObject.CompareTag("Player"))
            return true;

        // Enemy - check tag or EnemyBase component
        if ((targetMode == TargetMode.Enemy || targetMode == TargetMode.Both))
        {
            if (col.gameObject.CompareTag("Enemy")) return true;
            if (col.GetComponentInParent<EnemyBase>() != null) return true;
            if (col.GetComponentInParent<Health>() != null && !col.gameObject.CompareTag("Player")) return true; // generic health (avoid player)
        }

        return false;
    }

    void ApplyDamageToTarget(GameObject targetRoot)
    {
        if (targetRoot == null) return;

        // If target is player, prefer Health on PlayerMovement root
        if (targetMode == TargetMode.Player || targetMode == TargetMode.Both)
        {
            if (targetRoot.CompareTag("Player"))
            {
                var playerMovement = targetRoot.GetComponentInChildren<PlayerMovement>() ?? targetRoot.GetComponent<PlayerMovement>();
                Health hp = null;
                if (playerMovement != null)
                    hp = playerMovement.GetComponent<Health>() ?? playerMovement.GetComponentInChildren<Health>();
                if (hp == null)
                    hp = targetRoot.GetComponentInChildren<Health>() ?? targetRoot.GetComponent<Health>();

                if (hp != null)
                {
                    hp.TakeDmg(damagePerTick);
                    return;
                }
            }
        }

        // Try enemy handling (if enemy mode enabled)
        if (targetMode == TargetMode.Enemy || targetMode == TargetMode.Both)
        {
            // Prefer EnemyBase on root
            var enemyBase = targetRoot.GetComponentInChildren<EnemyBase>() ?? targetRoot.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                if (enemyBase is IDamageable dmgable)
                {
                    dmgable.TakeDmg(damagePerTick);
                    return;
                }

                var enemyHealth = enemyBase.GetComponent<EnemyHealth>() ?? enemyBase.GetComponentInChildren<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damagePerTick);
                    return;
                }

                var fallback = enemyBase.GetComponentInParent<Health>() ?? enemyBase.GetComponent<Health>();
                if (fallback != null)
                {
                    fallback.TakeDmg(damagePerTick);
                    return;
                }
            }

            // Generic IDamageable on root
            if (targetRoot.TryGetComponent<IDamageable>(out var anyDmg))
            {
                anyDmg.TakeDmg(damagePerTick);
                return;
            }

            // Last resort: any Health component on root
            var health = targetRoot.GetComponentInChildren<Health>() ?? targetRoot.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDmg(damagePerTick);
                return;
            }
        }
    }

    // Editor gizmo to show radius
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.15f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
