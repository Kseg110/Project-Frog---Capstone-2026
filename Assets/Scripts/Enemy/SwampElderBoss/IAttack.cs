using UnityEngine;

public interface IAttack
{
    float range { get; }
    string attackName { get; }
    float damage { get; }
    float cooldown { get; }

    double lastUsed { get; set; }


    void Attack(Transform target, Transform enemy);

    bool CanAttack(Transform target,Transform enemy);
}
