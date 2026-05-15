using UnityEngine;

/// <summary>
/// Trap that shoots a dart in a fixed direction and can optionally spawn a poison cloud.
/// </summary>
public class DartTrapEffect : TrapBase
{
    [SerializeField]
    private GameObject dartPrefab;

    [SerializeField]
    private Transform shootPoint;

    protected override void Awake()
    {
        base.Awake();

        if (dartPrefab == null)
        {
            Debug.LogError($"Class {nameof(DartTrap)} requires a dartPrefab reference.");
        }

        if (shootPoint == null)
        {
            Debug.LogError($"Class {nameof(DartTrap)} requires a shootPoint reference.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        ShootDart();
        SpawnPoisonCloud();
    }

    /// <summary>
    /// Instantiates and configures a dart projectile at the shoot point.
    /// </summary>
    private void ShootDart()
    {
        if (dartPrefab == null)
        {
            Debug.LogError($"Class {nameof(DartTrap)} cannot shoot because dartPrefab is null.");
            return;
        }

        if (shootPoint == null)
        {
            Debug.LogError($"Class {nameof(DartTrap)} cannot shoot because shootPoint is null.");
            return;
        }

        GameObject dartInstance = Instantiate(dartPrefab, shootPoint.position, shootPoint.rotation);

        DartProjectile dartProjectile = dartInstance.GetComponent<DartProjectile>();

        if (dartProjectile == null)
        {
            Debug.LogError($"Class {nameof(DartTrap)} requires the dartPrefab to have a {nameof(DartProjectile)} component.");
            return;
        }

        dartProjectile.CanApplyPoison = IsPoisonous;
    }
}
