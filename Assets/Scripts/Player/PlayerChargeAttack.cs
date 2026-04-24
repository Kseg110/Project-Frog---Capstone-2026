using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerChargeAttack : MonoBehaviour
{
    [Header("Charge Projectile Prefabs")]
    [SerializeField] private GameObject FireChargeProjectilePrefab;
    [SerializeField] private GameObject IceChargeProjectilePrefab;
    [SerializeField] private GameObject WindChargeProjectilePrefab;

    [Header("Charge Settings")]
    [SerializeField] private float MaxChargeTime = 1f;

    private AnchorBase CurrentAnchor;
    private float ChargeTimer;
    private bool isCharging;

    public bool IsCharging => isCharging;


    private void Awake()
    {
        if (FireChargeProjectilePrefab == null || IceChargeProjectilePrefab == null || WindChargeProjectilePrefab == null)
        {
            Debug.LogError($"Missing Projectile Prefab assignment within inspector");
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
        float ChargePercent = Mathf.Clamp01(ChargeTimer / MaxChargeTime);

        // Baseline values
        float ChargedDamage = 5f;
        int BaseWindProjectiles = 4;

        switch (CurrentAnchor.BaseData)
        {
            case AnchorFireData fireData:
                FireProjectile(
                    FireChargeProjectilePrefab,
                    FirePoint,
                    direction,
                    ChargedDamage,
                    fireData.BurnDuration,
                    fireData.BurnTickRate,
                    "Burn",
                    ChargePercent
                );
                break;

            case AnchorIceData iceData:
                float iceDamage = ChargedDamage * iceData.DamageMultiplier;
                FireProjectile(
                    IceChargeProjectilePrefab,
                    FirePoint,
                    direction,
                    iceDamage,
                    iceData.SlowDuration,
                    iceData.SlowMultiplier,
                    "Freeze",
                    ChargePercent
                );
                break;

            case AnchorWindData windData:
                int windProjectiles = BaseWindProjectiles;
                float windDamage = ChargedDamage * windData.DamageMultiplier;
                float spreadAngle = 5f;
                for (int i = 0; i < windProjectiles; i++)
                {
                    float angle = spreadAngle * i;
                    Vector3 spreadDir = Quaternion.Euler(0, angle, 0) * direction;
                    FireProjectile(
                        WindChargeProjectilePrefab,
                        FirePoint,
                        spreadDir,
                        windDamage,
                        0f,
                        0f,
                        null,
                        ChargePercent
                    );
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
