using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class MovementComponent : MonoBehaviour
{
    private NavMeshAgent agent;
    public NavMeshAgent Agent => agent;

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Movement Settings")]
    [SerializeField] private float stopDistance = 3f;
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float repathRate = 0.2f;  //how often the enemie's path is recalculated//

    private float repathTimer;
    private bool canMove = true;

    private void Awake()
    { 
        agent = GetComponent<NavMeshAgent>(); 
    }

    private void Start()
    {
        agent.stoppingDistance = stopDistance;
        agent.speed = movementSpeed;
    }

    private void Update()
    {
        if (!canMove || target == null) return;

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            MoveTo(target.position);
            repathTimer = repathRate;
        }
    }


    //change target//
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void MoveTo(Vector3 position)
    {
        if(!agent.enabled) return;

        agent.isStopped = false;
        agent.SetDestination(position);
    }

    public void Stop()
    {
        if (!agent.enabled) return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    public bool HasReachedDestination()
    {
        if(!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            return true;
        }
        return false;
    }

    public float GetDistanceToTarget()
    {
        if(target == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position,target.position);
    }

    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;

        if (!enabled)
            Stop();
    }

}
