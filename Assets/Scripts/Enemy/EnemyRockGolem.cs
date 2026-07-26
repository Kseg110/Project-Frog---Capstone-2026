using System.Collections;
using UnityEngine;

public class EnemyRockGolem : EnemyBase
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2f;

    private EnemyAttack enemyAttack;

    protected override void Awake()
    {
        base.Awake();
        enemyAttack = GetComponent<EnemyAttack>();
    }

    protected override void Update()
    {
        base.Update();

        if (player == null) return;

        if (enemyAttack.IsAttacking)
        {
            StopMovement();
            return;
        }

        HandleBehaviour();
    }

    private void HandleBehaviour() //if outside of range, chase the player, otherwise attack the player
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange)
        {
            ChasePlayer();
        }
        else
        {
            AttackPlayer();
        }
    }

    #region Behaviours
    protected void ChasePlayer()
    {
        movement.MoveToTarget(player.position);  
    }

    protected void AttackPlayer()
    {
        StopMovement();

        if (enemyAttack.CanAttack)
        {
            Debug.Log("[Golem] Calling TriggerAttack");
            enemyAttack.TriggerAttack(player.position);
        }
    }
    #endregion
}