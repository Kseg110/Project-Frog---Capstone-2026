using Assets.Scripts.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerImmortality))]
[RequireComponent(typeof(PlayerShieldController))]
public class PlayerTakeDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float immortalityTime = 1f;

    [Header("Knockback")]
    [SerializeField] private float knockbackSpeed = 20f;
    [SerializeField] private float knockbackEasePower = 2f;

    [Header("Visual Feedback")]
    [SerializeField] private float flashFrequency = 10f;

    [Tooltip("Create an Unlit/Lit solid Red material and drag it here in the Inspector.")]
    [SerializeField] private Material redFlashMaterial;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private string hitBoxName = "Hitbox";

    public bool isGod;

    // References
    private Health playerHealth;
    private PlayerMovement playerMovement;
    private PlayerImmortality playerImmortality;
    private PlayerShieldController shield;
    private Rigidbody rb;
    private CapsuleCollider hitbox;
    private PlayerAnimation playerAnimation;

    // Renderer and Material Caching
    private Renderer[] cachedRenderers;
    private Material[][] originalMaterials;

    // Timing & Handles
    private float nextAllowedDamageTime = 0f;
    private Coroutine flashCoroutine;
    private Coroutine knockbackCoroutine;

    private void Awake()
    {
        playerHealth = GetComponent<Health>();
        playerMovement = GetComponent<PlayerMovement>();
        playerImmortality = GetComponent<PlayerImmortality>();
        shield = GetComponent<PlayerShieldController>();
        rb = GetComponent<Rigidbody>();
        playerAnimation = GetComponentInChildren<PlayerAnimation>();

        Transform hit = transform.Find(hitBoxName);
        if (hit != null)
        {
            hitbox = hit.GetComponent<CapsuleCollider>();
        }

        CacheFlashRenderers();
        rb.isKinematic = true;
    }

    public void RefreshRenderers()
    {
        CacheFlashRenderers();
    }

    private void CacheFlashRenderers()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        // Get MeshRenderers AND SkinnedMeshRenderers (including inactive)
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);

        AnchorTether tether = GetComponentInChildren<AnchorTether>();
        Transform tetherRoot = tether != null ? tether.transform : null;

        List<Renderer> flashSet = new List<Renderer>();
        foreach (Renderer r in allRenderers)
        {
            if (tetherRoot != null && r.transform.IsChildOf(tetherRoot))
                continue;

            if (r is ParticleSystemRenderer || r is TrailRenderer)
                continue;

            flashSet.Add(r);
        }

        cachedRenderers = flashSet.ToArray();

        // Store original materials so we can restore them perfectly
        originalMaterials = new Material[cachedRenderers.Length][];
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            originalMaterials[i] = cachedRenderers[i].sharedMaterials;
        }
    }

    public void TryApplyDamageAndKnockback(float damageAmount, Vector3 knockDirection, float knockbackDistance)
    {
        if (isGod || Time.time < nextAllowedDamageTime)
            return;

        if (shield != null && shield.TakeDamage((int)damageAmount))
        {
            nextAllowedDamageTime = Time.time + immortalityTime;
            return;
        }

        if (playerImmortality != null && playerImmortality.IsImmortal)
            return;

        nextAllowedDamageTime = Time.time + Mathf.Max(0f, immortalityTime);

        if (playerHealth != null)
            playerHealth.TakeDmg(damageAmount);

        StartKnockback(knockDirection, knockbackDistance);
        StartFlash();
    }

    private void StartKnockback(Vector3 direction, float distance)
    {
        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine = StartCoroutine(KnockbackRoutine(direction.normalized, distance));

        if (playerAnimation != null)
            playerAnimation.PlayTakeDamage();
    }

    private IEnumerator KnockbackRoutine(Vector3 dir, float distance)
    {
        if (playerMovement != null)
            playerMovement.StopMovement();

        Vector3 start = rb.position;
        float duration = Mathf.Max(0.01f, distance / knockbackSpeed);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easedT = 1f - Mathf.Pow(1f - t, knockbackEasePower);

            Vector3 targetOffset = Vector3.Lerp(Vector3.zero, dir * distance, easedT);
            Vector3 desiredPosition = start + targetOffset;
            Vector3 motion = desiredPosition - rb.position;

            if (hitbox != null)
            {
                CollisionUtility.MoveWithCapsuleCollision(rb, hitbox, motion, collisionLayers);
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (playerMovement != null)
            playerMovement.ResumeMovement();

        knockbackCoroutine = null;
    }

    private void StartFlash()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheFlashRenderers();
        }

        if (cachedRenderers.Length == 0 || flashFrequency <= 0f)
            return;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float interval = 1f / flashFrequency / 2f;
        bool isRed = false;

        try
        {
            while (Time.time < nextAllowedDamageTime)
            {
                isRed = !isRed;
                SetFlash(isRed);
                yield return new WaitForSeconds(interval);
            }
        }
        finally
        {
            SetFlash(false);
            flashCoroutine = null;
        }
    }

    private void SetFlash(bool red)
    {
        if (cachedRenderers == null) return;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer r = cachedRenderers[i];
            if (r == null) continue;

            if (red && redFlashMaterial != null)
            {
                // Create an array matching the renderer's sub-mesh count filled with red material
                Material[] redMats = new Material[originalMaterials[i].Length];
                for (int m = 0; m < redMats.Length; m++)
                {
                    redMats[m] = redFlashMaterial;
                }
                r.sharedMaterials = redMats;
            }
            else
            {
                // Revert to original materials
                r.sharedMaterials = originalMaterials[i];
            }
        }
    }

    private void OnDisable()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        SetFlash(false);
    }
}