using UnityEngine;
using System;
using System.Collections;

public enum ShieldType
{
    None,
    Fire,
    Ice,
    Wind
}

public class PlayerShieldController : MonoBehaviour
{
    public event Action<ShieldType> OnShieldBroken;

    private ShieldType currentShield = ShieldType.None;

    // Cooldowns
    private bool fireReady = true;
    private bool iceReady = true;
    private bool windReady = true;

    private bool windBroken = false;
    private bool iceBroken = false;
    private bool fireBroken = false;

    // Fire break effects
    [SerializeField] private float fireBreakRadius = 5f;
    [SerializeField] private float fireBreakDamage = 15f;
    [SerializeField] private float fireBreakBurnDuration = 2f;
    [SerializeField] private float fireBreakBurnTickRate = 0.2f;
    [SerializeField] private float fireCooldown = 10f;
    [SerializeField] private GameObject fireBreakVFX;
    [SerializeField] private float fireBreakVFXDuration = 2f;

    // Ice break effects
    [SerializeField] private float iceCooldown = 10f;
    [SerializeField] private float iceBreakKnockbackRadius = 6f;
    [SerializeField] private float iceBreakFreezeDuration = 3f;
    [SerializeField] private GameObject iceBreakVFX;
    [SerializeField] private float iceBreakVFXDuration = 2f;

    // Wind break effects
    [SerializeField] private float windBreakKnockbackRadius = 5f;
    [SerializeField] private float windBreakKnockbackForce = 15f;
    [SerializeField] private float windSpeedBuffPercent = 35f;
    [SerializeField] private float windSpeedBuffDuration = 5f;
    [SerializeField] private float windCooldown = 10f;
    [SerializeField] private GameObject windBreakVFX;
    [SerializeField] private float windBreakVFXDuration = 2f;

    private PlayerAnchor anchor;
    private PlayerMovement movement;

    private void Awake()
    {
        anchor = FindFirstObjectByType<PlayerAnchor>();
        movement = FindFirstObjectByType<PlayerMovement>();
    }

    private void Update()
    {
        TryAutoReactivateShield();
    }

    // ============================================================
    // AUTO REACTIVATION
    // ============================================================

    private void TryAutoReactivateShield()
    {
        if (currentShield != ShieldType.None)
            return; // Shield is active, no need to reactivate

        // FIRE
        if (!fireBroken &&
            anchor != null &&
            anchor.IsTetherActive &&
            anchor.AttachedAnchor != null &&
            anchor.AttachedAnchor.Element == AnchorElement.Fire &&
            UpgradeManager.Instance.HasUpgrade("Fire Shield"))
        {
            GiveFireShield();
        }

        // ICE
        if (!iceBroken &&
            anchor != null &&
            anchor.IsTetherActive &&
            anchor.AttachedAnchor != null &&
            anchor.AttachedAnchor.Element == AnchorElement.Ice &&
            UpgradeManager.Instance.HasUpgrade("Ice Shield"))
        {
            GiveIceShield();
        }

        // WIND
        if (!windBroken &&
            anchor != null &&
            anchor.IsTetherActive &&
            anchor.AttachedAnchor != null &&
            anchor.AttachedAnchor.Element == AnchorElement.Wind &&
            UpgradeManager.Instance.HasUpgrade("Wind Shield"))
        {
            GiveWindShield();
        }
    }

    // ============================================================
    // PUBLIC API
    // ============================================================

    public void GiveFireShield()
    {
        if (!fireReady) return;

        currentShield = ShieldType.Fire;
    }

    public void GiveIceShield()
    {
        if (!iceReady) return;

        currentShield = ShieldType.Ice;
    }

    public void GiveWindShield()
    {
        if (!windReady) return;

        currentShield = ShieldType.Wind;
    }

    public void RemoveShield()
    {
        currentShield = ShieldType.None;
    }

    /// <summary>
    /// Returns true if the shield absorbed the hit.
    /// </summary>
    public bool TakeDamage(int dmg)
    {
        if (currentShield == ShieldType.None)
            return false; // no shield → player takes damage

        // Shield absorbs the hit
        ShieldType brokenType = currentShield;
        OnShieldBroken?.Invoke(brokenType);

        switch (brokenType)
        {
            case ShieldType.Fire:
                HandleFireDamage();
                break;

            case ShieldType.Ice:
                HandleIceDamage();
                break;

            case ShieldType.Wind:
                HandleWindDamage();
                break;
        }

        return true; // hit absorbed
    }

    // ============================================================
    // FIRE EXPLOSION
    // ============================================================

    private void HandleFireDamage()
    {
        if (fireBroken)
        {
            return;
        }

        fireBroken = true;

        ApplyFireBreakEffects();
        RemoveShield();

        StartCoroutine(FireShieldCooldownRoutine());
    }

    private void ApplyFireBreakEffects()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        Vector3 playerPos = anchor.transform.position;

        if (fireBreakVFX != null)
        {
            GameObject vfx = Instantiate(fireBreakVFX, playerPos, Quaternion.identity);
            Destroy(vfx, fireBreakVFXDuration);
        }

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            float dist = Vector3.Distance(playerPos, enemy.transform.position);
            if (dist > fireBreakRadius)
                continue;

            // DAMAGE
            enemy.TakeDamage(fireBreakDamage);

            // APPLY BURN
            enemy.TakeDamage(0f, "Burn", fireBreakBurnDuration, fireBreakBurnTickRate);

            // KNOCKBACK
            Vector3 dir = (enemy.transform.position - playerPos).normalized;
            dir.y = 0f;

            var knock = enemy.GetComponent<EnemyKnockback>();
            if (knock != null)
                knock.ApplyKnockback(dir, 5f);
        }
    }

    private IEnumerator FireShieldCooldownRoutine()
    {
        yield return new WaitForSeconds(fireCooldown);

        fireBroken = false;
    }

    // ============================================================
    // ICE EXPLOSION
    // ============================================================

    private void HandleIceDamage()
    {
        if (iceBroken)
        {
            return;
        }

        iceBroken = true;

        ApplyIceBreakEffects();
        RemoveShield();

        StartCoroutine(IceShieldCooldownRoutine());
    }

    private void ApplyIceBreakEffects()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        Vector3 playerPos = anchor.transform.position;

        if (iceBreakVFX != null)
        {
            GameObject vfx = Instantiate(iceBreakVFX, playerPos, Quaternion.identity);
            Destroy(vfx, iceBreakVFXDuration);
        }

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            float dist = Vector3.Distance(playerPos, enemy.transform.position);
            if (dist > iceBreakKnockbackRadius)
                continue;

            // Knockback
            Vector3 dir = (enemy.transform.position - playerPos).normalized;
            dir.y = 0f;

            var knock = enemy.GetComponent<EnemyKnockback>();
            if (knock != null)
                knock.ApplyKnockback(dir, 5f);

            // Freeze
            enemy.Freeze(iceBreakFreezeDuration);
        }
    }

    private IEnumerator IceShieldCooldownRoutine()
    {
        yield return new WaitForSeconds(iceCooldown);

        iceBroken = false;
    }

    // ============================================================
    // WIND BREAK EFFECTS
    // ============================================================

    private void HandleWindDamage()
    {
        if (windBroken)
        {
            return;
        }

        windBroken = true;

        ApplyWindBreakEffects();
        StartCoroutine(WindSpeedBuffRoutine());
        RemoveShield();

        StartCoroutine(WindShieldCooldownRoutine());
    }

    private void ApplyWindBreakEffects()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        Vector3 playerPos = anchor.transform.position;

        if (windBreakVFX != null)
        {
            GameObject vfx = Instantiate(windBreakVFX, playerPos, Quaternion.identity);
            Destroy(vfx, windBreakVFXDuration);
        }

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            float dist = Vector3.Distance(playerPos, enemy.transform.position);
            if (dist > windBreakKnockbackRadius)
                continue;

            Vector3 dir = (enemy.transform.position - playerPos).normalized;
            dir.y = 0f;

            var knock = enemy.GetComponent<EnemyKnockback>();
            if (knock != null)
                knock.ApplyKnockback(dir, 5f);
        }
    }

    private IEnumerator WindSpeedBuffRoutine()
    {
        float mult = 1f + (windSpeedBuffPercent / 100f);
        movement.AddSpeedModifier(this, mult);

        yield return new WaitForSeconds(windSpeedBuffDuration);

        movement.RemoveSpeedModifier(this);
    }

    private IEnumerator WindShieldCooldownRoutine()
    {
        yield return new WaitForSeconds(windCooldown);

        windBroken = false;
    }
}