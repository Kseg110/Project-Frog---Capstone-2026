using UnityEngine;

//Summary: A ranged enemy that inherits from EnemyBase and delegates all attack behaviour to a pluggable AttackBaseSO ScriptableObject. This is for the Axolotl! -E.M

public class EnemyAxolotl : EnemyBase
{
    [Header("Engagement Distances")]
    [Tooltip("The ideal distance the Axolotl tries to maintain from the player.")]
    [SerializeField] private float preferredDistance = 10f;

    [Tooltip("How far inside or outside preferredDistance is still acceptable. " +
             "The Axolotl won't move if it's within preferredDistance ± tolerance.")]
    [SerializeField] private float distanceTolerance = 1.5f;

    [Tooltip("If the player gets closer than this, the Axolotl backs away.")]
    [SerializeField] private float retreatDistance = 5f;

    [Header("Attack (Scriptable Object)")]
    [Tooltip("Drag any AttackBaseSO asset here — ranged, AoE, etc.")]
    [SerializeField] private AttackBaseSO attackSO;

    [Header("Rotation")]
    [Tooltip("How quickly the Axolotl turns to face the player while attacking.")]
    [SerializeField] private float lookRotationSpeed = 8f;

    protected override void Awake()
    {
        base.Awake();

        if (attackSO != null)
        {
            // Create a runtime clone so we don't write to the shared asset
            attackSO = Instantiate(attackSO);
        }
        else
        {
            Debug.LogError($"[EnemyAxolotl] No AttackBaseSO assigned on {gameObject.name}.");
        }
    }

    protected override void Update()
    {
        // Let the base class handle checks and such
        base.Update();

        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < retreatDistance)
        {
            // Run from the Player if they get close.
            Retreat();
        }
        else if (distanceToPlayer > preferredDistance + distanceTolerance)
        {
            // Close the distance to Player.
            Approach();
        }
        else
        {
            // Inside the comfort zone — hold position and face the player.
            StopMovement();
            FaceTarget();
        }

        // Attempt attack regardless of movement state.
        TryAttack();
    }

    // Walk toward the player, stopping once reaching the comfort zone.
    private void Approach()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 targetPosition = player.position - directionToPlayer * preferredDistance;
        MoveTo(targetPosition);
    }

    // Back away from the player.
    private void Retreat()
    {
        Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;
        Vector3 retreatTarget = player.position + directionAwayFromPlayer * preferredDistance;
        MoveTo(retreatTarget);
    }

    // Smoothly rotate to face the player on the Y axis only (no tilting).
    private void FaceTarget()
    {
        Vector3 lookDir = (player.position - transform.position);
        lookDir.y = 0f; // stay level
        if (lookDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * lookRotationSpeed
        );
    }

    // Delegates entirely to the ScriptableObject. 
    private void TryAttack()
    {
        if (attackSO == null) return;

        if (attackSO.CanAttack(player, transform))
        {
            attackSO.Attack(player, transform);
        }
    }
}

