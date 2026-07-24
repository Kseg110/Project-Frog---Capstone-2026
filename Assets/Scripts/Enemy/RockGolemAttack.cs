using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class RockGolemAttack : EnemyAttack
{
    [Header("Burrow Settings")]
    [SerializeField] private float burrowDepth = 2.5f;
    [SerializeField] private float travelSpeed = 8f;
    [SerializeField] private float maxTravelDuration = 5f;

    [Header("Emerge Settings")]
    [SerializeField] private float emergeSpeed = 12f;
    [SerializeField] private float postEmergePause = 0.5f;

    [Header("Visual Effects and Indications")]
    [SerializeField] private Collider[] golemColliders;
    [SerializeField] private LayerMask groundLayer;

    private Coroutine attackRoutine;
    private EnemyBase ownerEnemy;

    private void Awake()
    {
        ownerEnemy = GetComponent<EnemyBase>();
    }

    private void OnDisable()
    {
        CleanupAttack();
    }

    private void OnDestroy()
    {
        CleanupAttack();
    }

    protected override void OnExecuteAttack(Vector3 targetPosition)
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }

        attackRoutine = StartCoroutine(BurrowRoutine(targetPosition));
    }

    private IEnumerator BurrowRoutine(Vector3 targetPosition)
    {
        IsAttacking = true;

        // 1. Pause normal EnemyBase movement
        if (ownerEnemy != null)
        {
            ownerEnemy.StopMovement();
        }

        // 2. Hide visuals and colliders (submerge)
        SetGolemVisible(false);

        Vector3 startPos = transform.position;
        Vector3 undergroundStartPos = startPos - (Vector3.up * burrowDepth);
        transform.position = undergroundStartPos;

        // 3. Move Underground towards target
        float elapsed = 0f;

        while (elapsed < maxTravelDuration)
        {
            elapsed += Time.deltaTime;

            Vector3 currentGroundTarget = GetGroundPosition(targetPosition);
            Vector3 undergroundTarget = currentGroundTarget - (Vector3.up * burrowDepth);

            // Move the transform directly beneath the floor
            transform.position = Vector3.MoveTowards(transform.position, undergroundTarget, travelSpeed * Time.deltaTime);

            // Arrived underground
            if (Vector3.Distance(transform.position, undergroundTarget) < 0.2f)
            {
                break;
            }

            yield return null;
        }

        // 4. Emerge Sequence
        Vector3 emergeTargetPos = GetGroundPosition(transform.position);

        SetGolemVisible(true);

        while(Vector3.Distance(transform.position, emergeTargetPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, emergeTargetPos, emergeSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = emergeTargetPos;

        // 5. Post-attack pause and restore AI control
        yield return new WaitForSeconds(postEmergePause);

        if (ownerEnemy != null)
        {
            ownerEnemy.ResumeMovement();
        }

        IsAttacking = false;
        attackRoutine = null;
    }

    private void SetGolemVisible(bool visible)
    {
        if (golemColliders != null)
        {
            foreach (var c in golemColliders)
            {
                if (c != null) c.enabled = visible;
            }
        }
    }

    private Vector3 GetGroundPosition(Vector3 rawPos)
    {
        Vector3 rayStart = rawPos + Vector3.up * 10f;

        if (groundLayer != 0 && Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, groundLayer))
        {
            return hit.point;
        }

        return rawPos;
    }

    private void CleanupAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        SetGolemVisible(true);
        IsAttacking = false;
    }
}
