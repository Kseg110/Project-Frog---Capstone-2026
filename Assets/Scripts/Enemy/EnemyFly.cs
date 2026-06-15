using UnityEngine;
using UnityEngine.AI;

public class EnemyFly : MonoBehaviour
{

    [SerializeField] private float roamRadius = 5;
    [SerializeField] private float minMoveInterval = 1f;
    [SerializeField] private float maxMoveInterval = 3f;
    [SerializeField] private float flyHeight = 7f;

    private NavMeshAgent agent;
    private Vector3 centerPoint;
    private float moveTimer;
    private float nextMoveTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!agent.isOnNavMesh)
        {
            agent.Warp(transform.position);
       
        }
        agent.baseOffset = flyHeight;
        centerPoint = transform.position;

        SetNextMoveTime();
        PickNewDestination();
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 1)
        {
            moveTimer += Time.deltaTime;

            if (moveTimer >= nextMoveTime)
            {
                PickNewDestination();
                SetNextMoveTime();
            }
        }
    }

    void SetNextMoveTime()
    {
        moveTimer = 0f;
        nextMoveTime = Random.Range(minMoveInterval, maxMoveInterval);
    }
    void PickNewDestination()
    {
        Vector2 randomCircle = Random.insideUnitCircle * roamRadius;

        Vector3 randomPoint = centerPoint + new Vector3(
            randomCircle.x,
            0f,
            randomCircle.y
        );

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomPoint, out hit, roamRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // add visual of the roam area //
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? centerPoint : transform.position, roamRadius);
    }
}
