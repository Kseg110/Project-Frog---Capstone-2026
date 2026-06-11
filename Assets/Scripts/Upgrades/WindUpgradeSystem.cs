using UnityEngine;
using System.Collections;

public class WindUpgradeSystem : MonoBehaviour
{
    private PlayerShieldController shield;
    private PlayerAttacks playerAttacks;
    private PlayerChargeAttack chargeAttack;

    [SerializeField] private int debugExtraVolley;
    [SerializeField] private bool debugHomingEnabled;
    [SerializeField] private PlayerAnchor playerAnchor;

    private bool canTether = true;
    private float tetherCooldown = 1f;

    //wind shield specific
    private int windShieldHP = 0;
    private int maxWindShieldHP = 2;

    private void Awake()
    {
        shield = FindFirstObjectByType<PlayerShieldController>();
        playerAttacks = FindFirstObjectByType<PlayerAttacks>();
        chargeAttack = FindFirstObjectByType<PlayerChargeAttack>();
    }

    private void OnEnable()
    {
        if (playerAnchor == null)
        {
            Debug.LogError("[WindUpgradeSystem] PlayerAnchor reference is NULL!");
            return;
        }

        playerAnchor.OnTetherStarted += HandleAttach;
        playerAnchor.OnTetherReleased += HandleDetach;
        shield.OnShieldBroken += HandleShieldBreak;

        Debug.Log("[WindUpgradeSystem] Subscribed to PlayerAnchor events.");
    }

    private void OnDisable()
    {
        if (playerAnchor != null)
        {
            playerAnchor.OnTetherStarted -= HandleAttach;
            playerAnchor.OnTetherReleased -= HandleDetach;
        }

        shield.OnShieldBroken -= HandleShieldBreak;
        Debug.Log("[WindUpgradeSystem] Unsubscribed from PlayerAnchor events.");
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

        if (anchor.Element != AnchorElement.Wind)
            return;

        Debug.Log("[WIND] HandleAttach() CALLED");

        ApplyWindShield();
        ApplyRapidfire();
        ApplyMultishot();
        ApplyHomingDarts();
    }

    private void HandleDetach()
    {
        RemoveWindShield();
        ResetRapidfire();
        ResetMultishot();
        DisableHomingDarts();
    }

    // ============================
    // WIND SHIELD
    // ============================
    private void ApplyWindShield()
    {
        if (!UpgradeManager.Instance.HasUpgrade("Wind shield"))
            return;

        windShieldHP = maxWindShieldHP;
        shield.GiveShield();
    }

    private void RemoveWindShield()
    {
        windShieldHP = 0;
        shield.RemoveShield();
    }

    private void HandleShieldBreak()
    {
        if (windShieldHP > 0)
        {
            windShieldHP--;
            if (windShieldHP > 0)
            {
                shield.GiveShield();
                return;
            }
        }
    }

    // ============================
    // RAPIDFIRE (attack speed)
    // ============================
    private float baseAPS;

    private void ApplyRapidfire()
    {
        if (playerAttacks == null) return;

        if (baseAPS == 0)
            baseAPS = playerAttacks.attacksPerSecond;

        float bonus = UpgradeManager.Instance.GetTotalStatForElement(
            AnchorElement.Wind,
            UpgradeStat.AttackSpeed
        );

        playerAttacks.attacksPerSecond = baseAPS * (1f + bonus / 100f);
    }

    private void ResetRapidfire()
    {
        if (playerAttacks != null && baseAPS > 0)
            playerAttacks.attacksPerSecond = baseAPS;
    }

    // ============================
    // MULTISHOT (secondary volley)
    // ============================
    private int extraVolley = 0;

    private void ApplyMultishot()
    {
        Debug.Log("[WIND] ApplyMultishot() CALLED");

        extraVolley = (int)UpgradeManager.Instance.GetTotalStatForElement(
            AnchorElement.Wind,
            UpgradeStat.ExtraVolleyDarts
        );

        debugExtraVolley = extraVolley;
        Debug.Log($"[WIND] Extra volley = {extraVolley}");
    }

    private void ResetMultishot()
    {
        extraVolley = 0;
        debugExtraVolley = 0;
    }

    public int GetExtraVolley()
    {
        return extraVolley;
    }

    // ============================
    // HOMING DARTS
    // ============================
    private bool homingEnabled = false;

    private void ApplyHomingDarts()
    {
        homingEnabled = UpgradeManager.Instance.HasUpgrade("Homing Dart");
        debugHomingEnabled = homingEnabled;
    }

    private void DisableHomingDarts()
    {
        homingEnabled = false;
        debugHomingEnabled = false;
    }

    public bool IsHomingEnabled()
    {
        return homingEnabled;
    }
}