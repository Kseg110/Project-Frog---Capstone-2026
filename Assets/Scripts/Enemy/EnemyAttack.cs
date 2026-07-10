using System.Collections;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack Configuration")]
    [SerializeField] private GameObject attackHitBox;
    [SerializeField] private Transform attackPoint; //empty transform where the attack spawns
    [SerializeField] private float hitBoxLifeTime = 0.1f; //how long the attack lingers

    private GameObject currentHitBox; // prevent multiple hitboxes being created

    public bool IsAttacking { get; private set; } = false;

    public void TriggerAttack()
    {
        if (attackHitBox == null || attackPoint == null || IsAttacking) return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        IsAttacking = true;

        currentHitBox = Instantiate(attackHitBox, attackPoint.position, attackPoint.rotation);

        yield return new WaitForSeconds(hitBoxLifeTime);

        if (currentHitBox != null)
        {
            Destroy(currentHitBox);
            currentHitBox = null;
        }

        IsAttacking = false;
    }
}
