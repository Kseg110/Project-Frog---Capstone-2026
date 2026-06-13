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

    // Wind shield = multiple hits
    private int windCharges = 0;

    // Cooldowns
    private bool fireReady = true;
    private bool iceReady = true;

    private bool firePending = false;
    private bool icePending = false;
    private bool windPending = false;

    private float fireCooldown = 10f;
    private float iceCooldown = 10f;
    private float windCooldown = 10f;

    private PlayerAnchor anchor;

    private void Awake()
    {
        anchor = FindFirstObjectByType<PlayerAnchor>();
    }

    // ============================================================
    // PUBLIC API
    // ============================================================

    private void Update()
    {
        TryAutoReactivateShield();
    }

    private void TryAutoReactivateShield()
    {
        if (currentShield != ShieldType.None)
            return; // déjà un shield actif

        // FIRE
        if (firePending && fireReady)
        {
            GiveFireShield();
            firePending = false;
            return;
        }

        // ICE
        if (icePending && iceReady)
        {
            GiveIceShield();
            icePending = false;
            return;
        }

        // WIND
        if (windPending)
        {
            GiveWindShield(2);
            windPending = false;
            return;
        }
    }

    public void GiveFireShield()
    {
        if (!fireReady) return;

        currentShield = ShieldType.Fire;
        Debug.Log("Fire shield gained!");
    }

    public void GiveIceShield()
    {
        if (!iceReady) return;

        currentShield = ShieldType.Ice;
        Debug.Log("Ice shield gained!");
    }

    public void GiveWindShield(int charges)
    {
        windCharges = charges;
        currentShield = ShieldType.Wind;
        Debug.Log($"Wind shield gained ({charges} charges)!");
    }

    public void RemoveShield()
    {
        currentShield = ShieldType.None;
        windCharges = 0;
    }

    /// <summary>
    /// Returns true if the shield absorbed the hit.
    /// </summary>
    public bool TakeDamage(int dmg)
    {
        if (currentShield == ShieldType.None)
            return false; // no shield → player takes damage

        // Shield absorbs the hit
        Debug.Log($"[Shield] Hit absorbed by {currentShield} shield!");
        ShieldType brokenType = currentShield;
        OnShieldBroken?.Invoke(brokenType);

        switch (brokenType)
        {
            case ShieldType.Fire:
                TriggerFireExplosion();
                StartCoroutine(FireCooldownRoutine());
                RemoveShield();
                break;

            case ShieldType.Ice:
                TriggerIceExplosion();
                StartCoroutine(IceCooldownRoutine());
                RemoveShield();
                break;

            case ShieldType.Wind:
                windCharges--;
                if (windCharges > 0)
                {
                    Debug.Log($"Wind shield absorbed hit. {windCharges} charges left.");
                }
                else
                {
                    Debug.Log("Wind shield fully depleted.");
                    RemoveShield();
                    StartCoroutine(WindCooldownRoutine());
                }
                break;
        }

        return true; // hit absorbed
    }

    // ============================================================
    // FIRE EXPLOSION
    // ============================================================

    private void TriggerFireExplosion()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(anchor.transform.position, enemy.transform.position);
            if (dist <= 6f)
                enemy.TakeDamage(20f, "Burn", 2f, 0.2f);
        }

        Debug.Log("[Shield] Fire explosion triggered!");
    }

    // ============================================================
    // ICE EXPLOSION
    // ============================================================

    private void TriggerIceExplosion()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(anchor.transform.position, enemy.transform.position);
            if (dist <= 10f)
                enemy.Freeze(1f);
        }

        Debug.Log("[Shield] Ice freeze explosion triggered!");
    }

    // ============================================================
    // COOLDOWNS
    // ============================================================

    private IEnumerator FireCooldownRoutine()
    {
        fireReady = false;
        yield return new WaitForSeconds(fireCooldown);
        fireReady = true;
        firePending = true;
    }

    private IEnumerator IceCooldownRoutine()
    {
        iceReady = false;
        yield return new WaitForSeconds(iceCooldown);
        iceReady = true;
        icePending = true;
    }

    private IEnumerator WindCooldownRoutine()
    {
        windPending = false;
        yield return new WaitForSeconds(windCooldown);
        windPending = true;
    }
}