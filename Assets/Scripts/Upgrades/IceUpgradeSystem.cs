using UnityEngine;
using System.Collections;

public class IceUpgradeSystem : MonoBehaviour
{
    private PlayerShieldController shield;
    private AnchorTether tether;

    private bool iceShieldReady = true;
    private float tetherCooldown = 1.0f;
    private bool canTether = true;

    private const float freezeRadius = 10f;

    private void Awake()
    {
        shield = FindFirstObjectByType<PlayerShieldController>();
        tether = FindFirstObjectByType<AnchorTether>();
    }

    private void OnEnable()
    {
        tether.OnAnchorAttached += HandleAttach;
        tether.OnAnchorDetached += HandleDetach;
        shield.OnShieldBroken += HandleShieldBreak;
    }

    private void OnDisable()
    {
        tether.OnAnchorAttached -= HandleAttach;
        tether.OnAnchorDetached -= HandleDetach;
        shield.OnShieldBroken -= HandleShieldBreak;
    }

    private IEnumerator TetherCooldown()
    {
        canTether = false;
        yield return new WaitForSeconds(tetherCooldown);
        canTether = true;
    }

    private void HandleAttach(AnchorBase anchor)
    {
        if (!canTether) return;
        StartCoroutine(TetherCooldown());

        if (anchor.Element != AnchorElement.Ice)
            return;

        // ICE SHIELD
        if (UpgradeManager.Instance.HasUpgrade("Ice shield") && iceShieldReady)
        {
            shield.GiveShield();
            iceShieldReady = false;
        }
    }

    private void HandleDetach()
    {
        shield.RemoveShield();
        iceShieldReady = true;
    }

    private void HandleShieldBreak()
    {
        if (!UpgradeManager.Instance.HasUpgrade("Ice shield"))
            return;

        FreezeExplosion();
    }

    private void FreezeExplosion()
    {
        foreach (var enemy in FindObjectsOfType<EnemyBase>())
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist <= freezeRadius)
                enemy.Freeze(1f);
        }
    }

    public void OnHitEnemy(EnemyBase enemy, bool isPiercingHit)
    {
        // SHATTER
        if (UpgradeManager.Instance.HasUpgrade("Shatter") && enemy.IsFrozen)
        {
            enemy.TakeDamage(50);
            enemy.Cleanse();
        }

        // CRYO FRAGILITY
        if (UpgradeManager.Instance.HasUpgrade("Cryo Fragility") && enemy.IsSlowed)
        {
            float bonus = UpgradeManager.Instance.GetTotalStatForElement(
                AnchorElement.Ice,
                UpgradeStat.DamageTakenIfSlowed
            );
            enemy.TakeDamagePercent(bonus);
        }

        // LETHAL PIERCING
        if (isPiercingHit && UpgradeManager.Instance.HasUpgrade("Lethal Piercing"))
        {
            float bonus = UpgradeManager.Instance.GetTotalStatForElement(
                AnchorElement.Ice,
                UpgradeStat.PostPierceDamage
            );
            enemy.TakeDamagePercent(bonus);
        }
    }
}