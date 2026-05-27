using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttack", menuName = "Attacks/RangedAttack")]
public class RangedAttack : AttackBaseSO
{
    [SerializeField] private GameObject ProjectilePrefab;
    public override float range => 5f;

    public override string attackName =>"Pewpew";

    public override float damage => 5f;

    public override float cooldown => 1f;

    protected override void PerformAttack(Transform target, Transform enemy)
    {
        Vector3 direction = (target.position - enemy.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        GameObject projectile = Instantiate(ProjectilePrefab, enemy.position, rotation);
        Debug.Log("Attacked");
    }
}
