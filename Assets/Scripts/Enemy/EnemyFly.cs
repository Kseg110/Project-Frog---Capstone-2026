using UnityEngine;

public class EnemyFly : MonoBehaviour
{
    [SerializeField] private float roamRadius = 5;
    [SerializeField] private float minMoveInterval = 1f;
    [SerializeField] private float maxMoveInterval = 3f;
    [SerializeField] private float flyHeight = 7f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stoppingDistance = 0.5f;

    private Vector3 centerPoint;
    private Vector3 targetPosition;
    private float moveTimer;
    private float nextMoveTime;

    void Start()
    {
        // Set initial position with flyHeight
        transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        centerPoint = transform.position;

        SetNextMoveTime();
        PickNewDestination();
    }

    void Update()
    {
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (distanceToTarget <= stoppingDistance)
        {
            moveTimer += Time.deltaTime;

            if (moveTimer >= nextMoveTime)
            {
                PickNewDestination();
                SetNextMoveTime();
            }
        }
        else
        {
            // Move towards target
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Rotate towards target
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0; // Keep rotation horizontal only
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
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

        targetPosition = centerPoint + new Vector3(
            randomCircle.x,
            0f,
            randomCircle.y
        );
    }

    // add visual of the roam area //
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? centerPoint : transform.position, roamRadius);
        
        // Draw target position
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
        }
    }
}
