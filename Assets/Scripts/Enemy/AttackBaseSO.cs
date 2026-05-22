using UnityEngine;

[CreateAssetMenu(fileName = "AttackBaseSO", menuName = "Scriptable Objects/AttackBaseSO")]
public abstract class AttackBaseSO : ScriptableObject, IAttack
{
    public abstract float range { get; }

    public abstract string attackName { get; }

    public abstract float damage { get; }

    public abstract float cooldown { get; }

    public double lastUsed { get; set; }

    public void Attack(Transform target, Transform enemy)
    {
        PerformAttack(target,enemy);
        lastUsed = Time.time;
    }

    public virtual bool CanAttack(Transform target, Transform enemy)
    {
        return (Vector3.Distance(target.position, enemy.position) < range) && Time.time > lastUsed + cooldown;
        //check line of sight if needed//
    }

    protected abstract void PerformAttack(Transform target, Transform enemy);
}
