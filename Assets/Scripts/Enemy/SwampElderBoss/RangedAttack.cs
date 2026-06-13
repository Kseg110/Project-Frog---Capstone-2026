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

        GameObject projectile = Instantiate(ProjectilePrefab, enemy.position + direction * 1f, Quaternion.LookRotation(direction));

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = direction * 10f;

        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Init(damage, 3f);
            proj.isPlayerProjectile = false;
        }
    }
}
