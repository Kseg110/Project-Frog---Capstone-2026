using System.Collections;
using UnityEngine;

/// <summary>
/// Handles taking damage, i-frames, knockback, and flashing visuals for the player.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerImmortality))]
[RequireComponent(typeof(PlayerShieldController))]
public class PlayerTakeDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("How long after taking damage until the player can be damaged again (i-frames/cooldown).")]
    [SerializeField] private float immortalityTime = 1f;

    [Header("Knockback")]
    [Tooltip("Knockback speed in meters per second.")]
    [SerializeField] private float knockbackSpeed = 20f;

    [Tooltip("Power of the knockback ease-out curve. Higher = snappier start, slower end.")]
    [SerializeField] private float knockbackEasePower = 2f;

    [Header("Visual Feedback")]
    [Tooltip("Red flashes per second during immortality time.")]
    [SerializeField] private float flashFrequency = 10f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers;

    [Tooltip("Child object name containing the capsule collider hitbox.")]
    [SerializeField] private string hitBoxName = "Hitbox";

    public bool isGod;

    // References
    private Health playerHealth;
    private PlayerMovement playerMovement;
    private PlayerImmortality playerImmortality;
    private PlayerShieldController shield;

    private Rigidbody rb;
    private CapsuleCollider hitbox;

    private Renderer[] cachedRenderers;
    private Color[] originalColors;

    // I-frame timing
    private float nextAllowedDamageTime = 0f;

    // Coroutine handles
    private Coroutine flashCoroutine;
    private Coroutine knockbackCoroutine;

    private void Awake()
    {
        // Cache references
        playerHealth = GetComponent<Health>();
        playerMovement = GetComponent<PlayerMovement>();
        playerImmortality = GetComponent<PlayerImmortality>();
        shield = GetComponent<PlayerShieldController>();
        rb = GetComponent<Rigidbody>();

        // Get hitbox collider from child object
        Transform hit = transform.Find(hitBoxName);

        if (hit == null)
        {
            Debug.LogError($"Hitbox child '{hitBoxName}' not found on {gameObject.name}");
            return;
        }

        hitbox = hit.GetComponent<CapsuleCollider>();

        if (hitbox == null)
        {
            Debug.LogError($"No CapsuleCollider found on hitbox object '{hitBoxName}'");
            return;
        }

        // Cache renderers
        cachedRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[cachedRenderers.Length];

        for (int i = 0; i < cachedRenderers.Length; i++)
            originalColors[i] = cachedRenderers[i].material.color;

        // Rigidbody handled manually
        rb.isKinematic = true;
    }

    /// <summary>
    /// Attempts to apply damage and knockback.
    /// </summary>
    public void TryApplyDamageAndKnockback(
        float damageAmount,
        Vector3 knockDirection,
        float knockbackDistance)
    {
        if (isGod)
            return;

        if (Time.time < nextAllowedDamageTime)
            return;

        // SHIELD ALWAYS CHECKS FIRST
        if (shield != null && shield.TakeDamage((int)damageAmount))
        {
            Debug.Log("[Shield] Hit absorbed by shield!");

            // Activate i-frames even if shield absorbed, to prevent multiple hits in quick succession
            nextAllowedDamageTime = Time.time + immortalityTime;
            return;
        }

        // If shield didn't absorb, check I-frames 
        if (playerImmortality.IsImmortal)
            return;

        // Start i-frames
        nextAllowedDamageTime = Time.time + Mathf.Max(0f, immortalityTime);

        // Otherwise → real damage
        playerHealth.TakeDmg(damageAmount);

        // Effects
        StartKnockback(knockDirection, knockbackDistance);
        StartFlash();
    }

    /// <summary>
    /// Starts knockback coroutine.
    /// </summary>
    private void StartKnockback(Vector3 direction, float distance)
    {
        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine =
            StartCoroutine(KnockbackRoutine(direction.normalized, distance));
    }

    /// <summary>
    /// Collision-safe knockback movement.
    /// </summary>
    private IEnumerator KnockbackRoutine(Vector3 dir, float distance)
    {
        playerMovement.StopMovement();

        Vector3 start = rb.position;

        float duration = Mathf.Max(0.01f, distance / knockbackSpeed);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // Ease-out interpolation
            float easedT =
                1f - Mathf.Pow(1f - t, knockbackEasePower);

            Vector3 targetOffset =
                Vector3.Lerp(Vector3.zero, dir * distance, easedT);

            Vector3 desiredPosition = start + targetOffset;

            Vector3 motion = desiredPosition - rb.position;

            // Collision-safe movement using HITBOX collider
            CollisionUtility.MoveWithCapsuleCollision(
                rb,
                hitbox,
                motion,
                collisionLayers
            );

            elapsed += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        playerMovement.ResumeMovement();

        knockbackCoroutine = null;
    }

    /// <summary>
    /// Starts flashing visuals.
    /// </summary>
    private void StartFlash()
    {
        if (cachedRenderers.Length == 0 || flashFrequency <= 0f)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    /// <summary>
    /// Flashes player renderers during i-frames.
    /// </summary>
    private IEnumerator FlashRoutine()
    {
        float interval = 1f / flashFrequency / 2f;

        bool isRed = false;

        while (Time.time < nextAllowedDamageTime)
        {
            isRed = !isRed;

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                cachedRenderers[i].material.color =
                    isRed ? Color.red : originalColors[i];
            }

            yield return new WaitForSeconds(interval);
        }

        // Restore colors
        for (int i = 0; i < cachedRenderers.Length; i++)
            cachedRenderers[i].material.color = originalColors[i];

        flashCoroutine = null;
    }
}