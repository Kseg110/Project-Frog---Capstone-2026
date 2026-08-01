using System.Collections;
using UnityEngine;

public class FrogMeleeAttack : EnemyAttack
{
    [Header("Frog Melee Configuration")]
    [SerializeField] private GameObject attackHitBox;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float hitBoxLifeTime = 0.1f;

    // Added: damage and optional knockback so overlap check can apply proper effects.
    [SerializeField] private float meleeDamage = 10f;
    [SerializeField] private float meleeKnockbackDistance = 2f;

    protected override void OnExecuteAttack(Vector3 targetPosition)
    {
        StartCoroutine(MeleeRoutine());
    }

    private IEnumerator MeleeRoutine()
    {
        IsAttacking = true;

        if (attackHitBox != null && attackPoint != null)
        {
            GameObject currentHitBox = Instantiate(attackHitBox, attackPoint.position, attackPoint.rotation);

            // Immediate, deterministic overlap check to avoid relying only on OnTrigger events.
            Collider hbCollider = currentHitBox.GetComponent<Collider>();
            float checkRadius = 0.6f;

            if (hbCollider != null)
            {
                // Estimate a radius from common collider types and the hitbox scale
                Vector3 scale = currentHitBox.transform.lossyScale;
                if (hbCollider is SphereCollider sc)
                {
                    checkRadius = sc.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                }
                else if (hbCollider is CapsuleCollider cc)
                {
                    // approximate with larger dimension
                    checkRadius = Mathf.Max(cc.radius, cc.height * 0.5f) * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                }
                else if (hbCollider is BoxCollider bc)
                {
                    checkRadius = bc.size.magnitude * 0.5f * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                }
            }

            // Query triggers too so we detect trigger-based hitboxes and player colliders
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, checkRadius, ~0, QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                if (hit == null) continue;

                // Player: prefer PlayerTakeDamage to preserve knockback and i-frames
                if (hit.gameObject.CompareTag("Player"))
                {
                    var playerTake = hit.GetComponentInParent<PlayerTakeDamage>() ?? hit.GetComponent<PlayerTakeDamage>();
                    if (playerTake != null)
                    {
                        Vector3 dir = (hit.transform.position - transform.position);
                        dir.y = Mathf.Max(dir.y, 0.2f);
                        playerTake.TryApplyDamageAndKnockback(meleeDamage, dir.normalized, meleeKnockbackDistance);
                        continue;
                    }

                    var health = hit.GetComponentInParent<Health>() ?? hit.GetComponent<Health>();
                    if (health != null)
                    {
                        health.TakeDmg(meleeDamage);
                        continue;
                    }
                }

            
            }

            Destroy(currentHitBox, hitBoxLifeTime);
        }

        yield return new WaitForSeconds(hitBoxLifeTime);
        IsAttacking = false;
    }
}
