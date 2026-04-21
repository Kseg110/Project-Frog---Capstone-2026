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
        float BaseEffectDuration = 5f;
        int BaseWindProjectiles = 4;

        switch (CurrentAnchor.BaseData)
        {
            case AnchorFireData FireData:
                float BurnDuration = Mathf.Max(BaseEffectDuration, FireData.BurnDuration);
                FireProjectile(FireChargeProjectilePrefab, FirePoint, direction, ChargedDamage, BurnDuration, "Burn", ChargePercent);
                break;
            case AnchorIceData IceData:
                float IceDamage = ChargedDamage * IceData.DamageMultiplier;
                float FreezeDuration = Mathf.Max(BaseEffectDuration, IceData.SlowDuration);
                FireProjectile(IceChargeProjectilePrefab, FirePoint, direction, ChargedDamage, FreezeDuration, "Freeze", ChargePercent);
                break;
            case AnchorWindData WindData:
                int WindProjectiles = BaseWindProjectiles;
                float WindDamage = ChargedDamage * WindData.DamageMultiplier; 
                for (int i = 0; i < WindProjectiles; i++)
                {
                    FireProjectile(WindChargeProjectilePrefab, FirePoint, direction, ChargedDamage, 0f, null, ChargePercent);
                }
                break;
        }
        CancelCharge();
    }

    private void FireProjectile(GameObject prefab, Vector3 position, Vector3 direction, float damage, float EffectDuration, string effect, float ChargePercent)
    {
        var ProjObj = Instantiate(prefab, position, Quaternion.LookRotation(direction));
        var Proj = ProjObj.GetComponent<Projectile>();
        if (Proj != null)
        {
            Proj.Initialize(ChargePercent);
            Proj.damage = damage;
        }
        // derieved projectile effects here
    }

}
