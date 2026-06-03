using UnityEngine;
using System.Collections;

public class IceUpgradeSystem : MonoBehaviour
{
    private PlayerShieldController shield;
    private AnchorTether tether;

    private bool iceShieldReady = true;
    private float tetherCooldown = 1.0f;

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

    private bool canTether = true;

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

        if (UpgradeManager.Instance.HasUpgrade("Ice shield") && iceShieldReady)
        {
            shield.GiveShield();
            iceShieldReady = false;
        }
    }

    private void HandleDetach()
    {
        shield.RemoveShield();
    }

    private void HandleShieldBreak()
    {
        if (UpgradeManager.Instance.HasUpgrade("Ice shield"))
        {
            FreezeAllEnemies();
        }
    }

    private void FreezeAllEnemies()
    {
        foreach (var enemy in FindObjectsOfType<Enemy>())
            enemy.Freeze(2f);
    }

    // SHATTER
    public void OnHitEnemy(Enemy enemy)
    {
        if (UpgradeManager.Instance.HasUpgrade("Shatter") && enemy.IsFrozen)
        {
            enemy.TakeDamage(50);
            enemy.Cleanse();
        }

        // CRYO FRAGILITY
        if (UpgradeManager.Instance.HasUpgrade("Cryo Fragility") && enemy.IsSlowed)
        {
            float bonus = UpgradeManager.Instance.GetTotalStatForElement(AnchorElement.Ice, UpgradeStat.DamageTakenIfSlowed);
            enemy.TakeDamagePercent(bonus);
        }
    }
}