using System.Collections;
using UnityEngine;

/// <summary>
/// BoulderTrap: when the child trigger is activated this will spawn/activate a boulder and send it rolling.
/// The boulder deals damage and knockback to players/enemies on contact. It will slow down over time and also
/// slow when hitting walls.
/// </summary>
public class BoulderTrap : MonoBehaviour
{
    [Header("Boulder")]
    [Tooltip("Optional existing boulder object (child). If empty, prefab will be instantiated when triggered.")]
    public GameObject boulderObject;
    [Tooltip("Optional boulder prefab to instantiate on trigger (used if boulderObject is null).")]
    public GameObject boulderPrefab;
    [Tooltip("Local spawn offset applied when instantiating prefab.")]
    public Vector3 spawnOffset = Vector3.zero;
    [Tooltip("When true prefer using an existing child boulder in the scene instead of instantiating a prefab. If enabled the trap will look for a child named 'Boulder' or tagged 'Boulder'.")]
    public bool useChildBoulderOnly = true;

    [Header("Movement")]
    [Tooltip("Direction (local space) the boulder will roll.")]
    public Vector3 rollDirection = Vector3.forward;
    [Tooltip("Impulse force applied to start the boulder rolling.")]
    public float rollForce = 8f;

    [Header("Damage")]
    public float damage = 20f;
    public float knockbackForce = 8f;
    public bool damagePlayers = true;
    public bool damageEnemies = true;

    [Header("Slowdown")]
    [Tooltip("Number of seconds over which the boulder slows to a stop (applies increasing linear drag). Set 0 to disable time-based slowdown.")]
    public float slowDuration = 4f;
    [Tooltip("Multiplier applied to velocity when hitting a wall (instant slow).")]
    [Range(0f, 1f)]
    public float wallImpactSlowFactor = 0.5f;

    [Header("Collision")]
    [Tooltip("Layer mask used to detect walls for impact slowdown. Leave empty to treat all collisions except Player/Enemy as walls.")]
    public LayerMask wallLayers = ~0;

    [Header("Trigger")]
    [Tooltip("Child object tag to use as the trigger that activates this trap.")]
    public string triggerTag = "trap";

    [Header("Optional Prefab")]
    [Tooltip("Optional trigger prefab. If set, the prefab will be instantiated as a child and used as the trigger.")]
    public GameObject triggerPrefab;
    [Tooltip("Local position to place the instantiated trigger prefab (when used).")]
    public Vector3 triggerLocalPosition = Vector3.zero;

    private Transform spawnTransform;
    private Transform triggerInstance;
    private bool hasActivated = false;

    private void Start()
    {
        // If a trigger prefab is provided, instantiate it as a child and use it as the trigger
        Transform triggerTransform = null;
        if (triggerPrefab != null)
        {
            var inst = Instantiate(triggerPrefab, transform);
            inst.transform.localPosition = triggerLocalPosition;

            // Sanitize instantiated trigger prefab: remove any visible renderers/meshes so it doesn't show as a cube,
            // ensure colliders are triggers and remove any rigidbody which could cause physics artifacts.
            foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>(true))
            {
                // remove renderer to avoid visible geometry (prefab should ideally be collider-only)
                DestroyImmediate(mr);
            }
            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
            {
                DestroyImmediate(mf);
            }
            foreach (var sr in inst.GetComponentsInChildren<SpriteRenderer>(true))
            {
                DestroyImmediate(sr);
            }

            // Ensure all colliders are triggers and remove any rigidbodies on the trigger prefab
            foreach (var col in inst.GetComponentsInChildren<Collider>(true))
            {
                if (col != null)
                    col.isTrigger = true;
            }
            foreach (var rbChild in inst.GetComponentsInChildren<Rigidbody>(true))
            {
                DestroyImmediate(rbChild);
            }

            triggerTransform = inst.transform;
            Debug.Log($"BoulderTrap: Instantiated trigger prefab '{inst.name}' and sanitized visuals/rigidbodies.");
        }

        // If no prefab instance, find a child by tag
        if (triggerTransform == null)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject != gameObject && t.gameObject.CompareTag(triggerTag))
                {
                    triggerTransform = t;
                    break;
                }
            }
        }

        if (triggerTransform != null)
        {
            var forward = triggerTransform.GetComponent<BoulderTrapTriggerForwarder>() ?? triggerTransform.gameObject.AddComponent<BoulderTrapTriggerForwarder>();
            forward.parent = this;
            triggerInstance = triggerTransform;
        }

        // spawnTransform defaults to this transform
        spawnTransform = transform;

        // If configured to use a child boulder only, try to find one under this trap
        if (useChildBoulderOnly)
        {
            // If boulderObject is assigned but refers to a prefab asset (not in-scene), ignore it and search children
            if (boulderObject != null && !boulderObject.scene.IsValid())
            {
                boulderObject = null;
            }

            if (boulderObject == null)
            {
                // try by name then by tag
                Transform child = transform.Find("Boulder");
                if (child != null)
                    boulderObject = child.gameObject;
                else
                {
                    foreach (Transform t in GetComponentsInChildren<Transform>(true))
                    {
                        if (t.gameObject != gameObject && t.gameObject.CompareTag("Boulder"))
                        {
                            boulderObject = t.gameObject;
                            break;
                        }
                    }
                }
            }

            if (boulderObject == null)
            {
                Debug.LogWarning($"[BoulderTrap] useChildBoulderOnly is set but no child boulder was found under '{name}'. The trap will not spawn.");
            }
        }
    }

    internal void OnChildTriggerEnter(Collider other)
    {
        // Only react to player stepping on trigger to start boulder
        if (hasActivated) return;
        if (other.GetComponentInParent<PlayerMovement>() == null && !other.CompareTag("Player"))
            return;

        ActivateBoulder();
    }

    private void ActivateBoulder()
    {
        // Prevent repeated activations
        if (hasActivated) return;

        // This trap now requires a scene boulder (no prefab instantiation).
        if (boulderObject == null)
        {
            Debug.LogWarning($"[{nameof(BoulderTrap)}] No scene boulder assigned on {name}. The trap will not activate.");
            return;
        }

        GameObject b = boulderObject;

        // Ensure collider exists
        var col = b.GetComponent<Collider>();
        if (col == null)
            col = b.AddComponent<SphereCollider>();

        // Ensure Rigidbody exists (add only if missing)
        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"[{nameof(BoulderTrap)}] Scene boulder '{b.name}' has no Rigidbody. Adding one automatically.");
            rb = b.AddComponent<Rigidbody>();
        }

        rb.isKinematic = false;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Attach behaviour to handle collisions/damage/slowdown
        var behaviour = b.GetComponent<BoulderBehaviour>() ?? b.AddComponent<BoulderBehaviour>();
        behaviour.Initialize(this, rb, damage, knockbackForce, damagePlayers, damageEnemies, wallImpactSlowFactor, slowDuration, wallLayers);

        // Apply initial impulse in world-space direction
        Vector3 worldDir = spawnTransform.TransformDirection(rollDirection.normalized);
        rb.AddForce(worldDir * rollForce, ForceMode.Impulse);

        // mark activated and disable trigger so it cannot be reused
        hasActivated = true;
        if (triggerInstance != null)
        {
            var trigCol = triggerInstance.GetComponent<Collider>();
            if (trigCol != null)
                trigCol.enabled = false;
            else
                triggerInstance.gameObject.SetActive(false);
        }
    }

    // Small forwarder class placed on the child trigger to call back
    private class BoulderTrapTriggerForwarder : MonoBehaviour
    {
        public BoulderTrap parent;
        private void OnTriggerEnter(Collider other)
        {
            parent?.OnChildTriggerEnter(other);
        }
    }

    // Behaviour attached to the boulder instance to manage collisions and slowdown
    private class BoulderBehaviour : MonoBehaviour
    {
        private BoulderTrap config;
        private Rigidbody rb;
        private float damage;
        private float knockbackForce;
        private bool damagePlayers, damageEnemies;
        private float wallImpactSlowFactor;
        private float slowDuration;
        private LayerMask wallLayers;

        private Coroutine slowCoroutine;

        public void Initialize(BoulderTrap cfg, Rigidbody body, float dmg, float kb, bool dmgPlayers, bool dmgEnemies, float wallSlow, float slowDur, LayerMask walls)
        {
            config = cfg;
            rb = body;
            damage = dmg;
            knockbackForce = kb;
            damagePlayers = dmgPlayers;
            damageEnemies = dmgEnemies;
            wallImpactSlowFactor = Mathf.Clamp01(wallSlow);
            slowDuration = Mathf.Max(0f, slowDur);
            wallLayers = walls;

            if (slowCoroutine != null) StopCoroutine(slowCoroutine);
            if (slowDuration > 0f)
                slowCoroutine = StartCoroutine(SlowOverTime());
        }

        private IEnumerator SlowOverTime()
        {
            float elapsed = 0f;
            float startDrag = rb.linearDamping;
            float targetDrag = 2f; // arbitrary final drag to stop rolling
            while (elapsed < slowDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slowDuration);
                rb.linearDamping = Mathf.Lerp(startDrag, targetDrag, t);
                yield return null;
            }
            rb.linearDamping = targetDrag;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // slow on hitting wall layers
            if (((1 << collision.gameObject.layer) & wallLayers.value) != 0)
            {
                rb.linearVelocity *= wallImpactSlowFactor;
                // increase drag briefly
                if (slowCoroutine != null) StopCoroutine(slowCoroutine);
                StartCoroutine(TemporaryIncreaseDrag(1f, 0.5f));
            }

            // Damage any player or enemy hit
            foreach (var contact in collision.contacts)
            {
                var other = contact.otherCollider;
                if (other == null) continue;

                // Player
                if (damagePlayers)
                {
                    var playerTake = other.GetComponentInParent<PlayerTakeDamage>() ?? other.GetComponent<PlayerTakeDamage>();
                    if (playerTake != null)
                    {
                        Vector3 dir = (other.transform.position - transform.position).normalized;
                        dir.y = Mathf.Max(dir.y, 0.1f);
                        playerTake.TryApplyDamageAndKnockback(damage, dir, knockbackForce);
                        continue;
                    }

                    var playerMovement = other.GetComponentInParent<PlayerMovement>();
                    if (playerMovement != null)
                    {
                        var health = playerMovement.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
                        if (health != null) health.TakeDmg(damage);
                        ApplyKnockbackToRigidbody(other.GetComponentInParent<Rigidbody>(), other.transform.position);
                        continue;
                    }
                }

                // Enemy
                if (damageEnemies)
                {
                    if (other.TryGetComponent<EnemyBase>(out var enemyBase))
                    {
                        if (enemyBase is IDamageable dmgable)
                        {
                            dmgable.TakeDmg(damage);
                        }
                        else
                        {
                            var eh = enemyBase.GetComponent<EnemyHealth>();
                            if (eh != null) eh.TakeDamage(damage);
                        }
                        ApplyKnockbackToRigidbody(other.GetComponentInParent<Rigidbody>(), other.transform.position);
                        continue;
                    }

                    var parentEnemy = other.GetComponentInParent<EnemyBase>();
                    if (parentEnemy != null)
                    {
                        if (parentEnemy is IDamageable pd) pd.TakeDmg(damage);
                        else
                        {
                            var eh = parentEnemy.GetComponent<EnemyHealth>();
                            if (eh != null) eh.TakeDamage(damage);
                        }
                        ApplyKnockbackToRigidbody(other.GetComponentInParent<Rigidbody>(), other.transform.position);
                    }
                }

            }
        }

        private void ApplyKnockbackToRigidbody(Rigidbody targetRb, Vector3 hitPos)
        {
            if (targetRb == null) return;
            Vector3 push = (targetRb.position - transform.position).normalized * knockbackForce;
            if (targetRb.isKinematic)
            {
                targetRb.MovePosition(targetRb.position + push);
            }
            else
            {
                targetRb.AddForce(push, ForceMode.Impulse);
            }
        }

        private IEnumerator TemporaryIncreaseDrag(float toDrag, float duration)
        {
            float orig = rb.linearDamping;
            rb.linearDamping = toDrag;
            yield return new WaitForSeconds(duration);
            rb.linearDamping = orig;
        }
    }
}
