using UnityEngine;
using System.Collections;

public class WindUpgradeSystem : MonoBehaviour
{
    private PlayerShieldController shield;
    private AnchorTether tether;

    private bool windShieldReady = true;
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

        if (anchor.Element != AnchorElement.Wind)
            return;

        if (UpgradeManager.Instance.HasUpgrade("Wind shield") && windShieldReady)
        {
            shield.GiveShield();
            windShieldReady = false;
        }
    }

    private void HandleDetach()
    {
        shield.RemoveShield();
    }

    private void HandleShieldBreak()
    {
        if (UpgradeManager.Instance.HasUpgrade("Wind shield"))
        {
            ShootRadialBurst();
        }
    }

    private void ShootRadialBurst()
    {
        int count = 10;
        float angleStep = 36f;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep;
            // TODO: spawn dart projectile
            Debug.Log("Wind radial dart fired at angle " + angle);
        }
    }
}