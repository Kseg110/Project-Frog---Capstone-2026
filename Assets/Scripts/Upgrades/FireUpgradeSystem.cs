using UnityEngine;
using System.Collections;


public class FireUpgradeSystem : MonoBehaviour
{
    private PlayerShieldController shield;
    private AnchorTether tether;

    private bool fireShieldReady = true;
    private float fireShieldCooldown = 10f;

    private float tetherCooldown = 1.0f; // anti-abuse
    private bool canTether = true;

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

    private void HandleAttach(AnchorBase anchor)
    {
        if (!canTether) return; // anti-abuse
        StartCoroutine(TetherCooldown());

        if (anchor.Element != AnchorElement.Fire)
            return;

        // FIRE SHIELD
        if (UpgradeManager.Instance.HasUpgrade("Fire shield") && fireShieldReady)
        {
            shield.GiveShield();
            fireShieldReady = false;
            StartCoroutine(FireShieldCooldown());
        }
    }

    private void HandleDetach()
    {
        shield.RemoveShield();
    }

    private IEnumerator FireShieldCooldown()
    {
        yield return new WaitForSeconds(fireShieldCooldown);
        fireShieldReady = true;
    }

    private IEnumerator TetherCooldown()
    {
        canTether = false;
        yield return new WaitForSeconds(tetherCooldown);
        canTether = true;
    }

    private void HandleShieldBreak()
    {
        // Explosion effect
        if (UpgradeManager.Instance.HasUpgrade("Fire shield"))
        {
            SecondaryFireExplosion();
        }
    }

    private void SecondaryFireExplosion()
    {
        // TODO: call your existing secondary fire explosion logic
        Debug.Log("FIRE SHIELD EXPLOSION TRIGGERED");
    }

    //EXTINGUISHER
    public void OnHitEnemy(EnemyBase enemy)
    {
        if (!UpgradeManager.Instance.HasUpgrade("Extinguisher"))
            return;

        if (enemy.IsBurning)
        {
            enemy.TakeDamage(30);
            enemy.Cleanse();
        }
    }
}