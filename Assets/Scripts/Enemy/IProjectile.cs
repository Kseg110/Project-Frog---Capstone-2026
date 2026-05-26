// Implement this on any projectile prefab so that AttackBaseSO subclasses can pass damage and lifetime to the projectile at spawn time without needing to know the concrete projectile type. -E.M

public interface IProjectile
{
    void Init(float damage, float lifetime);
}
