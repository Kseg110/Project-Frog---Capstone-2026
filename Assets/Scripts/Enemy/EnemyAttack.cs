using UnityEngine;

public abstract class EnemyAttack : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] private float attackCoolDown = 5f;

    private float cooldownTimer = 0f;

    public bool IsAttacking { get; protected set; } = false;
    public bool CanAttack => !IsAttacking && cooldownTimer <= 0f;

    protected virtual void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    // Starts the attack sequence
    public void TriggerAttack(Vector3 targetPosition = default)
    {
        if (!CanAttack) return;

        cooldownTimer = attackCoolDown;
        OnExecuteAttack(targetPosition);
    }

    protected abstract void OnExecuteAttack(Vector3 targetPosition);
}
