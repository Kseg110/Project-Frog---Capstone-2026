using System.Collections;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public enum AttackType { MeleeHitBox, RockGolemAttackHitBox }

    [Header("Attack Style")]
    [SerializeField] private AttackType attackType = AttackType.MeleeHitBox;

    [Header("Attack Timing")]
    [SerializeField] private float attackCooldown = 5f;

    [Header("Skeleton Frog Attack Configuration")]
    [SerializeField] private GameObject attackHitBox;
    [SerializeField] private Transform attackPoint; //empty transform where the attack spawns
    [SerializeField] private float hitBoxLifeTime = 0.1f; //how long the attack lingers

    [Header("Rock Golem Attack Configuration")]
    [SerializeField] private GameObject previewPrefab;
    [SerializeField] private GameObject attackCylinderPrefab;
    [SerializeField] private float windupTime = 2f;
    [SerializeField] private float riseHeight = 3f;
    [SerializeField] private float riseSpeed = 6f;
    [SerializeField] private float lingeringDuration = 1f;

    private float cooldownTimer = 0f;

    public bool IsAttacking { get; private set; } = false;
    public bool CanAttack => !IsAttacking && cooldownTimer <= 0f;

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    // Call this method for Melee/Fixed Point Attacks
    public void TriggerAttack()
    {
        if (!CanAttack) return;
        if (attackType == AttackType.MeleeHitBox)
        {
            cooldownTimer = attackCooldown;
            StartCoroutine(MeleeRoutine());
        }
        else
        {
            Debug.LogWarning("TriggerAttack() called without a target position.", gameObject);
        }
    }

    // Call this method for targeted/location attacks
    public void TriggerAttack(Vector3 targetPosition)
    {
        if (!CanAttack) return;
        cooldownTimer = attackCooldown;
        if (attackType == AttackType.RockGolemAttackHitBox)
        {
            StartCoroutine(GolemRoutine(targetPosition));
        }
        else
        {
            StartCoroutine(MeleeRoutine());
        }
    }

    // Skeleton Frog Logic
    private IEnumerator MeleeRoutine()
    {
        IsAttacking = true;
        if (attackHitBox != null && attackPoint != null)
        {
            GameObject currentHitBox = Instantiate(attackHitBox, attackPoint.position, attackPoint.rotation);
            yield return new WaitForSeconds(hitBoxLifeTime);
            if (currentHitBox != null)
            {
                Destroy(currentHitBox);
            }
        }
        IsAttacking = false;
    }

    // Rock Golem Attack Logic
    private IEnumerator GolemRoutine(Vector3 targetPosition)
    {
        IsAttacking = true;

        // Determine strike position
        Vector3 strikePosition = targetPosition;
        strikePosition.y = 0f;

        // Spawn preview
        GameObject activePreview = null;
        if (previewPrefab != null)
        {
            activePreview = Instantiate(previewPrefab, strikePosition, Quaternion.identity);
        }

        // Wind-up delay
        yield return new WaitForSeconds(windupTime);

        // Remove Preview
        if (activePreview != null)
        {
            Destroy(activePreview);
        }

        // Spawn and Raise Cylinder
        if (attackCylinderPrefab != null)
        {
            // Spawn Attack cylinder below ground
            Vector3 spawnPosition = strikePosition - Vector3.up * riseHeight;
            GameObject cylinder = Instantiate(attackCylinderPrefab, spawnPosition, Quaternion.identity);

            float travelled = 0f;
            // Rise effect
            while (travelled < riseHeight)
            {
                float step = riseSpeed * Time.deltaTime;
                if (cylinder == null) break;
                cylinder.transform.position += Vector3.up * step;
                travelled += step;
                yield return null;
            }

            if (cylinder != null)
            {
                Destroy(cylinder, lingeringDuration);
            }
        }

        IsAttacking = false; // moved out of the null-check block so it always resets
    }
}