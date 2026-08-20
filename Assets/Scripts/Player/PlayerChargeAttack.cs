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

    [Header("References")]
    [SerializeField] private PlayerAnchor playerAnchor;

    private AnchorBase CurrentAnchor;
    private float ChargeTimer;
    private bool isCharging;
    private float cooldownTimer;
    private UIPlayerHUD playerHUD;

    private PlayerAttacks playerAttacks;

    public bool IsCharging => isCharging;
    public bool IsOnCooldown => cooldownTimer > 0f;
    public float CooldownProgress => Mathf.Clamp01(1f - (cooldownTimer / CooldownTime));

    private void Awake()
    {
        //if (FireChargeProjectilePrefab == null || IceChargeProjectilePrefab == null || WindChargeProjectilePrefab == null)
        //{
        //    Debug.LogError("[PlayerChargeAttack] Missing projectile prefab assignment!", this);
        //}

        // A charge attack is only ever fired from an active tether, so we need to know tether state.
        if (playerAnchor == null)
        {
            playerAnchor = GetComponent<PlayerAnchor>();
        }
        //if (playerAnchor == null)
        //{
        //    Debug.LogError("[PlayerChargeAttack] No PlayerAnchor reference — charge cannot be tether-gated!", this);
        //}

        playerHUD = FindAnyObjectByType<UIPlayerHUD>();
        playerAttacks = GetComponent<PlayerAttacks>();
    }

    private void OnEnable()
    {
        // Any release (Golem break, dash, manual detach, LOS break, out-of-range) routes through PlayerAnchor.ReleaseTether, which fires this. Cancelling here covers every break source with no per-source bookkeeping.
        if (playerAnchor != null)
            playerAnchor.OnTetherReleased += HandleTetherReleased;
    }

    private void OnDisable()
    {
        if (playerAnchor != null)
            playerAnchor.OnTetherReleased -= HandleTetherReleased;
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
        playerHUD?.UpdateChargeAttackCooldown(CooldownProgress);
    }

    // Fired when the tether is released by any means. If the player was mid-charge, drop it — no projectile, no cooldown penalty.
    private void HandleTetherReleased()
    {
        if (isCharging)
            CancelCharge();
    }

    public bool CanBeginCharge()
    {
        // Must be tethered AND settled to charge.
        bool tetherActive = playerAnchor != null && playerAnchor.IsTetherActive;
        return tetherActive && !IsOnCooldown && !isCharging;
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

        // Cancel if the tether stops being active mid-charge.
        if (playerAnchor == null || !playerAnchor.IsTetherActive)
        {
            CancelCharge();
            return;
        }

        ChargeTimer = Mathf.Clamp(ChargeTimer + Time.deltaTime, 0f, MaxChargeTime);
    }

    public void ReleaseCharge(Vector3 firePoint, Vector3 direction)
    {
        if (!IsCharging || CurrentAnchor == null) return;

        // Failsafe: even if a charge somehow survived a break or animation window this frame (event/order edge case), don't fire unless the tether is genuinely active (attached AND settled).
        if (playerAnchor == null || !playerAnchor.IsTetherActive)
        {
            CancelCharge();
            return;
        }

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

                    var proj = projObj.GetComponent<Projectile>() ?? projObj.GetComponentInChildren<Projectile>();
                    if (proj != null)
                    {
                        proj.isPlayerProjectile = true; // ensure this is set before anything that checks it
                        proj.player = this.gameObject;
                        proj.currentElement = AnchorElement.Fire;
                        //proj.pointBlankRange = playerAttacks.pointBlankRange;
                        proj.Initialize(chargePercent);
                        proj.damage = explosionDamage;
                        proj.effectType = "Burn";
                        proj.effectDuration = fireData.BurnDuration;
                        proj.effectValue = fireData.BurnTickRate;

                        // Apply charged knockback: scale from 1m to 5m with chargePercent
                        proj.knockbackDistance = Mathf.Lerp(1f, 5f, chargePercent);
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
                    var proj = projObj.GetComponent<Projectile>() ?? projObj.GetComponentInChildren<Projectile>();
                    if (proj != null)
                    {
                        proj.isPlayerProjectile = true;
                        proj.player = this.gameObject;
                        proj.currentElement = AnchorElement.Ice;
                        proj.pointBlankRange = playerAttacks.pointBlankRange;
                        proj.Initialize(chargePercent);
                        proj.damage = iceDamage;
                        proj.effectType = "Freeze";
                        proj.effectDuration = 1f;
                        proj.effectValue = 1f;
                        proj.isPiercingProjectile = true;

                        // Apply charged knockback
                        proj.knockbackDistance = Mathf.Lerp(1f, 5f, chargePercent);
                    }
                    else
                    {
                        //Debug.LogWarning($"[PlayerChargeAttack] IceChargeProjectilePrefab on {name} contains no Projectile component on root or children.");
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
                        var proj = projObj.GetComponent<Projectile>() ?? projObj.GetComponentInChildren<Projectile>();

                        if (proj != null)
                        {
                            proj.isPlayerProjectile = true;
                            proj.player = this.gameObject;
                            proj.currentElement = AnchorElement.Wind;
                            proj.pointBlankRange = playerAttacks.pointBlankRange;
                            proj.Initialize(chargePercent);
                            proj.damage = windDamage;

                            // Apply charged knockback (same per projectile)
                            proj.knockbackDistance = Mathf.Lerp(5f, 12f, chargePercent);

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