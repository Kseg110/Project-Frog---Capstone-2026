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
    public Transform Target => target;

    [SerializeField] private Transform player;


    [Header("Movement Settings")]
    [SerializeField] private float stopDistance = 3f;
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float repathRate = 0.2f;  //how often the enemie's path is recalculated//
    [SerializeField] private float rotationSpeed = 10f; //only affects rotation once enemy has reached destination
    [SerializeField] private float separationRadius = 5f;   //how far from the enemy an other enemy has to be to affect separation
    [SerializeField] private float separationStrength = 2f;  //how strongly this enemy pushes away from others

    [Header("Debug info")]
    [Tooltip("DO NOT MODIFY")]
    [SerializeField] private string currentSlot = "none";

    private float repathTimer;
    private bool canMove = true;

    [SerializeField] private LayerMask enemyLayer;
    private void Awake()
    {
        Debug.Log($"{name} Movement Awake");
        agent = GetComponent<NavMeshAgent>();

        player = GameObject.Find("Player").transform;
    }

    private void Start()
    {
        agent.stoppingDistance = stopDistance;
        agent.speed = movementSpeed;

        if (TargetManager.Instance == null)
        {
            Debug.LogError("No TargetManager found!");
            return;
        }
        target = TargetManager.Instance.RequestSlot(this);
    }

    private void Update()
    {
        if (!canMove || target == null) return;

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            MoveToTarget(target.position);
            repathTimer = repathRate;
        }

        if (HasReachedDestination())
        {
            RotateTowardsPlayer();
        }
    }


    //change target//
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        currentSlot = target != null ? target.name : "None";
    }

    public void MoveTo(Vector3 position) 
    {
        if (!agent.enabled) return;
        agent.isStopped = false;
        agent.SetDestination(position);
    }
    public void MoveToTarget(Vector3 movementTarget)
    {
        if (!agent.enabled || target == null) return;

        Vector3 targetDirection = movementTarget - transform.position;
        targetDirection.y = 0f;

        Vector3 separation = GetSeparationDirection();

        Vector3 finalDirection = targetDirection.normalized + separation * separationStrength;

        finalDirection.Normalize();

        agent.Move(finalDirection * movementSpeed * Time.deltaTime);
    }

    private void RotateTowardsPlayer()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation,targetRotation,rotationSpeed * Time.deltaTime);
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
    private Vector3 GetSeparationDirection()
    {
        Vector3 separation = Vector3.zero;

        Collider[] otherEnemies = Physics.OverlapSphere(transform.position,separationRadius,enemyLayer);

        foreach (Collider otherEnemy in otherEnemies)
        {
            if (otherEnemy.gameObject == gameObject) continue;

            Vector3 away = transform.position - otherEnemy.transform.position;

            float distance = away.magnitude;
  
            if (distance > 0.001f)
            {
                separation += away.normalized / distance;
            }
        }
        return separation;
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


    //remove self from movement target slot// CALL ON DEATH
    public void ReleaseTargetSlot()
    {
        TargetManager.Instance.ReleaseSlot(this);
        Debug.Log("Slot released");
    }
    public void RequestSlot()
    {
        target = TargetManager.Instance.RequestSlot(this);
        currentSlot = target != null ? target.name : "None";
        Debug.Log($"target set to : {(target != null ? target.name : "None")}");
    }
}
