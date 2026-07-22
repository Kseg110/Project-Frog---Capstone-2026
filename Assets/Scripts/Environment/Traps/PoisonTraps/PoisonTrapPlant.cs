using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PoisonTrap: Deals damage-over-time to Player, Enemy, or Both inside a radius.
/// - Ensures a trigger SphereCollider is present and sized to `radius`.
/// - Tracks occupants and applies damage every `tickInterval` seconds.
/// - Can toggle child ParticleSystem(s).
/// - Implements IDamageable so player projectiles that hit the plant will damage it and
///   trigger the plant to emit poison for `poisonDurationOnShot` seconds.
/// - Contains a physics fallback to detect fast projectiles that pass through triggers.
/// </summary>
public class PoisonTrapPlant : MonoBehaviour, IDamageable
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

    [Tooltip("Optional layer mask to restrict which colliders are considered (helps performance). Leave all layers to affect everything).")]
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

    [Header("Health (shootable)")]
    [Tooltip("Maximum health of the trap. When reduced to 0 the trap will be disabled/destroyed.")]
    public float maxHealth = 50f;

    [Tooltip("If true the trap GameObject will be destroyed when health reaches zero.")]
    public bool destroyOnDeath = true;

    [Tooltip("Delay in seconds before destroying the trap after health reaches zero.")]
    public float destroyDelayOnDeath = 0.0f;

    [Header("Poison-on-shot")]
    [Tooltip("Duration (seconds) the plant will emit poison when shot.")]
    public float poisonDurationOnShot = 5f;

    [Header("Projectile detection fallback")]
    [Tooltip("If true use an OverlapSphere to detect player projectiles that pass through the collider.")]
    public bool enableProjectileOverlapDetection = true;

    [Tooltip("Radius used by the overlap detection for projectiles (set roughly to your visual hit area).")]
    public float projectileDetectRadius = 0.5f;

    [Tooltip("How many colliders to allocate buffer for when doing OverlapSphereNonAlloc.")]
    public int overlapBufferSize = 8;

    [Header("Debug")]
    [Tooltip("Enable/disable debug gizmos for poison area.")]
    public bool showGizmos = true;

    [Tooltip("Show the poison area (outer radius) gizmo.")]
    public bool showPoisonAreaGizmo = true;

    // Internal: track occupants and next time they should take damage
    readonly Dictionary<GameObject, float> occupantsNextTick = new Dictionary<GameObject, float>();

    SphereCollider triggerCollider;
    ParticleSystem[] particleSystems;

    // Health
    private float currentHealth;
    private bool isDestroyed = false;

    // Overlap detection buffer
    private Collider[] overlapBuffer;

    // Poison active flag started by shot
    private bool poisonActiveFromShot = false;
    private Coroutine poisonCoroutine;

    void Start()
    {
        EnsureTriggerCollider();
        CacheParticleSystems();

        // Initialize health
        currentHealth = maxHealth;

        // Prepare overlap buffer
        overlapBuffer = new Collider[Mathf.Max(1, overlapBufferSize)];

        // By default disable particles until trap is explicitly toggled on (if enabled in inspector).
        if (particleSystems != null && particleSystems.Length > 0)
            SetParticlesActive(false);
    }

    void FixedUpdate()
    {
        // Use FixedUpdate for physics-related manual detection
        if (enableProjectileOverlapDetection && !isDestroyed)
            DetectProjectilesManually();
    }

    void Update()
    {
        if (isDestroyed) return;

        // While the plant is actively emitting poison due to being shot, skip the occupant tick processing
        // because the shot-poison coroutine is handling periodic damage to all targets inside the radius.
        if (poisonActiveFromShot)
            return;

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
    }

    void CacheParticleSystems()
    {
        if (particleRoot != null)
            particleSystems = particleRoot.GetComponentsInChildren<ParticleSystem>(true);
        else
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
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
                go.SetActive(false);
            }
        }
    }

    // Called by Unity when something enters the trigger
    void OnTriggerEnter(Collider other)
    {
        if (isDestroyed) return;
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
            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        }
    }

    // Support non-trigger collisions as well (forward to trigger handling)
    void OnCollisionEnter(Collision collision)
    {
        if (isDestroyed) return;
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
        if (isDestroyed) return;
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

    // ============================================================
    // IDamageable implementation - allow projectiles / player to damage plant directly.
    // When shot, the plant will activate its poison for `poisonDurationOnShot`.
    // ============================================================
    public void TakeDmg(float dmg)
    {
        if (isDestroyed) return;

        currentHealth -= dmg;
        // Activate poison emission when shot
        ActivatePoison(poisonDurationOnShot);

        if (currentHealth <= 0f)
            HandleDeath();
    }

    public void TakeDmg(float dmg, string effectTytpe, float effectDuration, float effectValue)
    {
        // Default: just take the numeric damage. Effects can be handled here if desired.
        TakeDmg(dmg);
    }

    private void HandleDeath()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // Stop doing DOT logic and visuals
        occupantsNextTick.Clear();
        if (triggerCollider != null) triggerCollider.enabled = false;
        if (particleSystems != null && particleSystems.Length > 0)
            SetParticlesActive(false);

        // Stop any active poison coroutine
        if (poisonCoroutine != null)
        {
            StopCoroutine(poisonCoroutine);
            poisonCoroutine = null;
            poisonActiveFromShot = false;
        }

        // Optionally destroy the GameObject or just disable it
        if (destroyOnDeath)
        {
            Destroy(gameObject, Mathf.Max(0f, destroyDelayOnDeath));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // Manual detection fallback: detect player projectiles overlapping a small radius and apply damage.
    void DetectProjectilesManually()
    {
        if (projectileDetectRadius <= 0f) return;

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, projectileDetectRadius, overlapBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            var col = overlapBuffer[i];
            if (col == null) continue;

            // Try to find a Projectile component on the collider or its parents
            var proj = col.GetComponent<Projectile>() ?? col.GetComponentInParent<Projectile>();
            if (proj != null && proj.isPlayerProjectile)
            {
                // Apply damage and destroy projectile to mimic normal projectile behavior
                TakeDmg(proj.damage);
                Destroy(proj.gameObject);
            }

            // Clear buffer entry to avoid stale references
            overlapBuffer[i] = null;
        }
    }

    // Activates the poison emission for `duration` seconds. While active the coroutine will
    // periodically apply `damagePerTick` to all valid targets inside `radius`.
    public void ActivatePoison(float duration)
    {
        if (isDestroyed) return;
        if (duration <= 0f) return;

        if (poisonCoroutine != null)
            StopCoroutine(poisonCoroutine);
        poisonCoroutine = StartCoroutine(PoisonBurstCoroutine(duration));
    }

    private IEnumerator PoisonBurstCoroutine(float duration)
    {
        poisonActiveFromShot = true;

        if (enableParticlesWhenActive && particleSystems != null && particleSystems.Length > 0)
            SetParticlesActive(true);

        float elapsed = 0f;
        while (elapsed < duration && !isDestroyed)
        {
            // Find all colliders in radius, apply damage to unique roots that are valid targets
            Collider[] cols = Physics.OverlapSphere(transform.position, radius);
            var hitRoots = new HashSet<GameObject>();
            foreach (var col in cols)
            {
                if (col == null) continue;
                var root = col.transform.root.gameObject;
                if (root == null) continue;
                if (!hitRoots.Add(root)) continue;
                if (!IsInAffectLayer(root)) continue;
                if (!IsValidTarget(col)) continue;

                ApplyDamageToTarget(root);
            }

            yield return new WaitForSeconds(Mathf.Max(0.01f, tickInterval));
            elapsed += tickInterval;
        }

        poisonActiveFromShot = false;

        if (enableParticlesWhenActive && particleSystems != null && particleSystems.Length > 0)
            SetParticlesActive(false);

        poisonCoroutine = null;
    }

    // Draw debug gizmos when enabled in inspector
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (showPoisonAreaGizmo)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.15f);
            Gizmos.DrawSphere(transform.position, Mathf.Max(0.01f, radius));
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, radius));
        }

        if (enableProjectileOverlapDetection && projectileDetectRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.15f);
            Gizmos.DrawSphere(transform.position, projectileDetectRadius);
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 1f);
            Gizmos.DrawWireSphere(transform.position, projectileDetectRadius);
        }
    }
}