using System.Collections;
using UnityEngine;

public class EnemyRockGolem : EnemyBase
{
    /*
    [Header("Attacks settings")]
    [SerializeField] private float AttackRange = 2f;
    [SerializeField] private float AttackCooldown = 5f;
    [SerializeField] private float WindupTime = 2f;
    [SerializeField] private float RiseHeight = 3f;
    [SerializeField] private float RiseSpeed = 6f;

    [Header("Golem Prefabs")]
    [SerializeField] private GameObject PreviewPrefab;
    [SerializeField] private GameObject AttackCylinderPrefab;
    
    protected bool CanAttack = true;
    private GameObject ActivePreview;
    */

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 5f;

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
        MoveTo(player.position);  
    }

    protected void AttackPlayer()
    {
        StopMovement();

        if (canAttack)
        {
            enemyAttack.TriggerAttack(player.position);
        }
    }
    #endregion

    /*
    #region attack coroutine
    private IEnumerator AttackRoutine()
    {
        CanAttack = false;

        // Stop movement 
        agent.isStopped = true;

        // Determine strike position 
        Vector3 StrikePosition = player.position;
        StrikePosition.y = 0f;

        // Spawn preview
        ActivePreview = Instantiate(PreviewPrefab, StrikePosition, Quaternion.identity);

        // Wind-up delay
        yield return new WaitForSeconds(WindupTime);

        // Remove Preview
        if (ActivePreview != null)
            Destroy(ActivePreview);

        // Spawn Attack cylinder below ground 
        Vector3 SpawnPosition = StrikePosition - Vector3.up * RiseHeight;
        GameObject cylinder = Instantiate(AttackCylinderPrefab, SpawnPosition, Quaternion.identity);

        // Rise effect 
        float Travelled = 0f;
        while (Travelled < RiseHeight)
        {
            float Step = RiseSpeed * Time.deltaTime;
            cylinder.transform.position += Vector3.up * Step;
            Travelled += Step;
            yield return null;
        }
        Destroy(cylinder, 1f);

        // Re-enable Movement
        agent.isStopped = false;

        // Cooldown
        yield return new WaitForSeconds(AttackCooldown);
        CanAttack = true;
    }
    #endregion
    */
}