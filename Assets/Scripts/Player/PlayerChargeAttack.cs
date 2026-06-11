using System;
using UnityEngine;

public class PlayerChargeAttack : MonoBehaviour
{
    [Header("Charge Projectile Prefabs")]
    [SerializeField] private GameObject FireChargeProjectilePrefab;    //prefab ???
    [SerializeField] private GameObject IceChargeProjectilePrefab;
    [SerializeField] private GameObject WindChargeProjectilePrefab;

    [Header("Charge Settings")]
    [SerializeField] private float MaxChargeTime = 1f;

    private AnchorBase CurrentAnchor;
    private float ChargeTimer;
    private bool isCharging;

    private FireUpgradeSystem fireSystem;
    private IceUpgradeSystem iceSystem;
    private WindUpgradeSystem windSystem;

    public bool IsCharging => isCharging;


    private void Awake()
    {
        if (FireChargeProjectilePrefab == null || IceChargeProjectilePrefab == null || WindChargeProjectilePrefab == null)
        {
            Debug.LogError("Missing Projectile Prefab assignment within inspector", this);
        }

        //Get FireUpgradeSystem reference
        fireSystem = FindFirstObjectByType<FireUpgradeSystem>();
        if (fireSystem == null)
        {
            Debug.LogError("FireUpgradeSystem not found in scene!", this);
        }

        //Get IceUpgradeSystem reference
        iceSystem = FindFirstObjectByType<IceUpgradeSystem>();
        if (iceSystem == null)
        {
            Debug.LogError("IceUpgradeSystem not found in scene!", this);
        }

        //Get WindUpgradeSystem reference 
        windSystem = FindFirstObjectByType<WindUpgradeSystem>();

        if (windSystem == null)
        {
            Debug.LogError("WindUpgradeSystem not found in scene!", this);
        }
    }

    public void BeginCharge(AnchorBase anchor)
    {
        CurrentAnchor = anchor;
        isCharging = true;
        ChargeTimer = 0f;
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

    public void ReleaseCharge(Vector3 FirePoint, Vector3 direction)
    {
        if (!IsCharging || CurrentAnchor == null) return;

        float chargePercent = Mathf.Clamp01(ChargeTimer / MaxChargeTime);

        // Baseline values
        float chargedDamage = 10f + 15f * chargePercent;

        switch (CurrentAnchor.BaseData)
        {
            // ---------------------------------------------------------
            // FIRE CHARGE ATTACK
            // ---------------------------------------------------------
            case AnchorFireData fireData:
                FireProjectile(
                    FireChargeProjectilePrefab,
                    FirePoint,
                    direction,
                    chargedDamage,
                    fireData.BurnDuration,
                    fireData.BurnTickRate,
                    "Burn",
                    chargePercent
                );
                break;

            // ---------------------------------------------------------
            // ICE CHARGE ATTACK
            // ---------------------------------------------------------

            case AnchorIceData iceData:
                {
                    float iceDamage = chargedDamage * iceData.DamageMultiplier;

                    var projObj = Instantiate(
                        IceChargeProjectilePrefab,
                        FirePoint,
                        Quaternion.LookRotation(direction)
                    );

                    var proj = projObj.GetComponent<Projectile>();
                    if (proj != null)
                    {
                        proj.Initialize(chargePercent);
                        proj.damage = iceDamage;

                        // ICE EFFECTS
                        proj.effectType = "Freeze";
                        proj.effectDuration = 1f;       // Freeze 1s
                        proj.effectValue = 1f;

                        // IMPORTANT: ICE SECONDARY = PIERCING
                        proj.isPiercingProjectile = true;
                    }
                    break;
                }

            // ---------------------------------------------------------
            // WIND CHARGE ATTACK
            // ---------------------------------------------------------
            case AnchorWindData windData:
                {
                    int baseProjectiles = 4;

                    // MULTISHOT UPGRADE
                    int extra = windSystem != null ? windSystem.GetExtraVolley() : 0;
                    int totalProjectiles = baseProjectiles + extra;

                    float windDamage = chargedDamage * windData.DamageMultiplier;
                    float spreadAngle = 5f;

                    for (int i = 0; i < totalProjectiles; i++)
                    {
                        float angle = spreadAngle * (i - totalProjectiles / 2f);
                        Vector3 spreadDir = Quaternion.Euler(0, angle, 0) * direction;

                        var projObj = Instantiate(WindChargeProjectilePrefab, FirePoint, Quaternion.LookRotation(spreadDir));
                        var proj = projObj.GetComponent<Projectile>();

                        if (proj != null)
                        {
                            proj.Initialize(chargePercent);
                            proj.damage = windDamage;

                            // HOMING UPGRADE
                            if (windSystem != null && windSystem.IsHomingEnabled())
                                proj.EnableHoming();
                        }
                    }
                }
                break;
        }
        CancelCharge();
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
