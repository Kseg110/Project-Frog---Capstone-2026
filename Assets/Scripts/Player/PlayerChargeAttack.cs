using System.Collections.Generic;
using UnityEngine;

public class PlayerChargeAttack : MonoBehaviour
{
    [Header("Charge Projectile Prefabs")]
    [SerializeField] private GameObject FireChargeProjectilePrefab;
    [SerializeField] private GameObject IceChargeProjectilePrefab;
    [SerializeField] private GameObject WindChargeProjectilePrefab;

    [Header("Charge Settings")]
    [SerializeField] private float MaxChargeTime = 1f;
    [SerializeField] private float CooldownTime = 1f;

    [Header("Damage Settings")]
    [SerializeField] private float MinDamage = 12.5f; // half damage on quick release
    [SerializeField] private float MaxDamage = 25f; // full damage on max charge held-release

    [Header("Charge Upgrade Settings")]
    [SerializeField] private float WindHomingDelay = 3f; // Delay before homing activates

    private AnchorBase CurrentAnchor;
    private float ChargeTimer;
    private bool isCharging;
    private float cooldownTimer;
    private UIPlayerHUD playerHUD;

    public bool IsCharging => isCharging;
    public bool IsOnCooldown => cooldownTimer > 0f;
    public float CooldownProgress => Mathf.Clamp01(1f - (cooldownTimer / CooldownTime));

    private void Awake()
    {
        if (FireChargeProjectilePrefab == null || IceChargeProjectilePrefab == null || WindChargeProjectilePrefab == null)
        {
            Debug.LogError("[PlayerChargeAttack] Missing projectile prefab assignment!", this);
        }
        playerHUD = FindAnyObjectByType<UIPlayerHUD>();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
        playerHUD?.UpdateChargeAttackCooldown(CooldownProgress);
    }

    public bool CanBeginCharge()
    {
        return !IsOnCooldown && !isCharging;
    }

    public bool BeginCharge(AnchorBase anchor)
    {
        if (!CanBeginCharge())
            return false;

        CurrentAnchor = anchor;
        isCharging = true;
        ChargeTimer = 0f;
        return true;
    }

    public void CancelCharge()
    {
        isCharging = false;
        ChargeTimer = 0f;
        CurrentAnchor = null;
    }    

    public void UpdateCharge()
    {
        if (!IsCharging || CurrentAnchor == null) return;
        ChargeTimer = Mathf.Clamp(ChargeTimer + Time.deltaTime, 0f, MaxChargeTime);
    }

    public void ReleaseCharge(Vector3 firePoint, Vector3 direction)
    {
        if (!IsCharging || CurrentAnchor == null) return;

        float chargePercent = Mathf.Clamp01(ChargeTimer / MaxChargeTime);
        float chargedDamage = Mathf.Lerp(MinDamage, MaxDamage, chargePercent);

        switch (CurrentAnchor.BaseData)
        {
            // ---------------------------------------------------------
            // FIRE CHARGE ATTACK
            // ---------------------------------------------------------
            case AnchorFireData fireData:
                {
                    float explosionDamage = chargedDamage;

                    if (PyronovaUpgrade.Instance != null)
                    {
                        float bonus = PyronovaUpgrade.Instance.GetExplosionBonus();
                        explosionDamage *= 1f + bonus / 100f;
                    }

                    var projObj = Instantiate(FireChargeProjectilePrefab, firePoint, Quaternion.LookRotation(direction));
                    var proj = projObj.GetComponent<Projectile>();
                    if (proj != null)
                    {
                        proj.Initialize(chargePercent);
                        proj.damage = explosionDamage;
                        proj.effectType = "Burn";
                        proj.effectDuration = fireData.BurnDuration;
                        proj.effectValue = fireData.BurnTickRate;
                        proj.isPlayerProjectile = true;
                    }

                    IgnorePlayerCollision(projObj);
                    break;
                }

            // ---------------------------------------------------------
            // ICE CHARGE ATTACK
            // ---------------------------------------------------------
            case AnchorIceData iceData:
                {
                    float iceDamage = chargedDamage * iceData.DamageMultiplier;

                    var projObj = Instantiate(IceChargeProjectilePrefab, firePoint, Quaternion.LookRotation(direction));
                    var proj = projObj.GetComponent<Projectile>();
                    if (proj != null)
                    {
                        proj.Initialize(chargePercent);
                        proj.damage = iceDamage;
                        proj.effectType = "Freeze";
                        proj.effectDuration = 1f;
                        proj.effectValue = 1f;
                        proj.isPiercingProjectile = true;
                        proj.isPlayerProjectile = true;
                    }

                    IgnorePlayerCollision(projObj);
                    break;
                }

            // ---------------------------------------------------------
            // WIND CHARGE ATTACK
            // ---------------------------------------------------------
            case AnchorWindData windData:
                {
                    int baseProjectiles = 4;
                    int extra = MultishotUpgrade.Instance != null ? MultishotUpgrade.Instance.GetExtraDarts() : 0;
                    int totalProjectiles = baseProjectiles + extra;

                    float windDamage = chargedDamage * windData.DamageMultiplier;
                    float spreadAngle = 5f;

                    List<GameObject> spawnedProjectiles = new List<GameObject>();

                    for (int i = 0; i < totalProjectiles; i++)
                    {
                        float angle = spreadAngle * (i - totalProjectiles / 2f);
                        Vector3 spreadDir = Quaternion.Euler(0, angle, 0) * direction;
                        Vector3 spawnPos = firePoint + spreadDir * 0.5f;

                        var projObj = Instantiate(WindChargeProjectilePrefab, spawnPos, Quaternion.LookRotation(spreadDir));
                        var proj = projObj.GetComponent<Projectile>();
                        
                        if (proj != null)
                        {
                            proj.Initialize(chargePercent);
                            proj.damage = windDamage;
                            proj.isPlayerProjectile = true;

                            if (HomingDartsUpgrade.Instance != null && HomingDartsUpgrade.Instance.IsEnabled())
                                proj.EnableHomingDelayed(WindHomingDelay);
                        }

                        IgnorePlayerCollision(projObj);

                        Collider[] projCols = projObj.GetComponentsInChildren<Collider>();
                        foreach (var other in spawnedProjectiles)
                        {
                            Collider[] otherCols = other.GetComponentsInChildren<Collider>();
                            foreach (var c1 in projCols)
                                foreach (var c2 in otherCols)
                                    Physics.IgnoreCollision(c1, c2);
                        }

                        spawnedProjectiles.Add(projObj);
                    }
                    break;
                }
        }
        // Start cooldown timer after releasing charge
        cooldownTimer = CooldownTime;
        CancelCharge();
    }

    // ============================================================
    // FAIL-SAFE : IGNORE PLAYER COLLISION FOR CHARGED PROJECTILES
    // ============================================================
    private void IgnorePlayerCollision(GameObject projObj)
    {
        var proj = projObj.GetComponent<Projectile>();
        if (proj == null || !proj.isPlayerProjectile)
            return;

        Collider[] projCols = projObj.GetComponentsInChildren<Collider>();
        Collider[] playerCols = GetComponentsInChildren<Collider>();

        foreach (var pCol in projCols)
            foreach (var col in playerCols)
                Physics.IgnoreCollision(pCol, col);
    }

    // Helper FireProjectile method accepts all effect parameters for each anchor type
    private void FireProjectile(
        GameObject prefab,
        Vector3 position,
        Vector3 direction,
        float damage,
        float effectDuration,
        float effectValue,
        string effect,
        float chargePercent)
    {
        var projObj = Instantiate(prefab, position, Quaternion.LookRotation(direction));
        var proj = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(chargePercent);
            proj.damage = damage;
            proj.effectType = effect;
            proj.effectDuration = effectDuration;
            proj.effectValue = effectValue;
        }
    }

}