using UnityEngine;
using UnityEngine.AI;

public class EnemyFly : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float waitTime = 2f;

    private NavMeshAgent agent;
    private int currentPointIndex = 0;
    private float waitTimer;
    private bool waiting;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!agent.isOnNavMesh)
        {
            agent.Warp(transform.position);
       
        }
        agent.baseOffset = 1f;
        GoToNextPoint();
    }

    void Update()
    {
        if (waiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                waiting = false;
                GoToNextPoint();
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waiting = true;
            waitTimer = 0f;
        }
    }

    void GoToNextPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPointIndex].position);

        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        Debug.Log("Setting destination: " + patrolPoints[currentPointIndex].position);
    }
}
