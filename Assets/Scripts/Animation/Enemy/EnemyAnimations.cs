using UnityEngine;

public class EnemyAnimations : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyBase enemy;

    [Header("Animator Parameters")]
    [SerializeField] private string isAttackingParameter = "IsAttacking";
    [SerializeField] private string movementSpeedParameter = "MovementSpeed";

    private int isAttackingHash;
    private int movementSpeedHash;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (enemy == null)
            enemy = GetComponent<EnemyBase>();

        isAttackingHash = Animator.StringToHash(isAttackingParameter);
        movementSpeedHash = Animator.StringToHash(movementSpeedParameter);
    }

    private void Update()
    {
        if (animator == null || enemy == null)
            return;

        // Attack
        animator.SetBool(
            isAttackingHash,
            enemy.IsAttacking
        );

        // Movement speed: 0 = stopped, 1 = full speed
        animator.SetFloat(
            movementSpeedHash,
            enemy.MovementSpeed
        );
    }
}