using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(EnemyAttack))]
public class EnemyFrogSkeleton : EnemyBase
{
    [Header("Attack config")]
    [SerializeField] private float attackRange = 1f;

    //[SerializeField] private Transform attackPoint; //empty transform where the attack spawns
    //[SerializeField] private float hitboxLifetime = 0.1f; //how long the attack lingers

    //private GameObject currentHitbox; //prevent multiple hitboxes being created

    private EnemyAttack enemyAttack;

    protected override void Awake()
    {
        base.Awake();
        enemyAttack = GetComponent<EnemyAttack>();
    }

    protected override void Update()
    {
        base.Update();

        if (player == null)
        {
            Debug.Log("player missing");
            return;
        }

        if (enemyAttack.IsAttacking) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < attackRange)
        {
            StopMovement();

            if (CanAttack)
            {
                enemyAttack.TriggerAttack();
            }
            return;
        }
        else
        {
            movement.MoveToTarget(movement.Target.position);
        }        
    }
/*
    private void Attack()
    {
        if (attackHitbox == null || attackPoint == null) return;
        if (currentHitbox != null) return;

        currentHitbox = Instantiate(
            attackHitbox,
            attackPoint.position,
            attackPoint.rotation
            );

        StartCoroutine(DestroyHitbox(hitboxLifetime));
    }

    private IEnumerator DestroyHitbox(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentHitbox != null)
        {
            Destroy(currentHitbox);
            currentHitbox = null;
        }
    }
*/
}
