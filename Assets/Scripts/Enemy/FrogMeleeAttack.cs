using System.Collections;
using UnityEngine;

public class FrogMeleeAttack : EnemyAttack
{
    [Header("Frog Melee Configuration")]
    [SerializeField] private GameObject attackHitBox;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float hitBoxLifeTime = 0.1f;

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
            Destroy(currentHitBox, hitBoxLifeTime);
        }

        yield return new WaitForSeconds(hitBoxLifeTime);
        IsAttacking = false;
    }
}
