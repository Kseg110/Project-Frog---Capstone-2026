// AnchorDamageManager
// Applies periodic tower-type effects (Fire, Ice, Wind) to nearby enemies while the player
// is actively grappling. Uses an overlap sphere to detect enemies within range and delegates
// to the appropriate effect logic based on the current tower's type.

using System.Collections;
using UnityEngine;

public class AnchorDamageManager : MonoBehaviour
{
    [Header("References")]
    public PlayerGrapple playerGrapple;

    [Header("Damage Settings")]
    public float damageRadius = 20f;
    public LayerMask enemyLayer;

    [Header("Effect Timing")]
    public float effectInterval = 10f;

    private float effectTimer;

    void Update()
    {
        if (playerGrapple == null || !playerGrapple.IsGrappling)
            return;

        GrappleTowerManager tower = playerGrapple.CurrentTower;
        if (tower == null) return;

        effectTimer += Time.deltaTime;

        if (effectTimer >= effectInterval)
        {
            ApplyEffect(tower);
            effectTimer = 0f;
        }
    }

    private void ApplyEffect(GrappleTowerManager tower)
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            damageRadius,
            enemyLayer
        );

        foreach (Collider hit in hits)
        {
            Health enemy = hit.GetComponent<Health>();
            if (enemy == null || enemy.IsDead) continue;

            switch (tower.TowerType)
            {
                case TowerType.Fire:
                    StartCoroutine(ApplyBurn(enemy, tower.FireFields));
                    break;

                case TowerType.Ice:
                    ApplyIce(enemy, tower.IceFields);
                    break;

                case TowerType.Wind:
                    ApplyWind(enemy, tower.WindFields);
                    break;
            }
        }
    }

    // ---------------- FIRE ----------------
    private IEnumerator ApplyBurn(Health enemy, FireTowerFields fire)
    {
        float timer = 0f;

        while (timer < fire.BurnDuration && enemy != null && !enemy.IsDead)
        {
            enemy.TakeDmg(fire.Damage);
            yield return new WaitForSeconds(fire.BurnTickRate);
            timer += fire.BurnTickRate;
        }
    }

    // ---------------- ICE ----------------
    private void ApplyIce(Health enemy, IceTowerFields ice)
    {
        float iceDamage = ice.Damage * ice.DamageMultiplier;
        enemy.TakeDmg(iceDamage);

        EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            enemyBase.ApplySlow(ice.SlowMultiplier, ice.SlowDuration);
        }
    }

    // ---------------- WIND ----------------
    private void ApplyWind(Health enemy, WindTowerFields wind)
    {
        float windDamage = wind.Damage * wind.DamageMultiplier;
        enemy.TakeDmg(windDamage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, damageRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
