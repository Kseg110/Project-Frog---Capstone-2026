using UnityEngine;

[RequireComponent(typeof(EnemyAttack))]
public class EnemyFrogSkeleton : EnemyBase
{
    [Header("Attack config")]
    [SerializeField] private float attackRange = 1f;

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
}
