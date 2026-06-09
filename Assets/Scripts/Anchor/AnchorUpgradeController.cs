using System;
using UnityEngine;

public class AnchorUpgradeController : MonoBehaviour
{
    private AnchorBase anchor;
    private AnchorData data;

    private void Awake()
    {
        anchor = GetComponent<AnchorBase>();
        data = anchor.BaseData;
    }

    private void OnEnable()
    {
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradesChanged += ApplyUpgrades;
    }

    private void OnDisable()
    {
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradesChanged -= ApplyUpgrades;
    }

    private void Start()
    {
        ApplyUpgrades();
    }

    private void ApplyUpgrades()
    {
        var element = anchor.BaseData switch
        {
            AnchorFireData => AnchorElement.Fire,
            AnchorIceData => AnchorElement.Ice,
            AnchorWindData => AnchorElement.Wind,
            _ => AnchorElement.Fire
        };

        float totalDamage = UpgradeManager.Instance.GetTotalStatForElement(element, UpgradeStat.PrimaryDamage);
        float attackSpeed = UpgradeManager.Instance.GetTotalStatForElement(element, UpgradeStat.AttackSpeed);
        float explosionDamage = UpgradeManager.Instance.GetTotalStatForElement(element, UpgradeStat.ExplosionDamage);

        // Apply to AnchorData
        data.Damage = data.Damage + totalDamage;
    }
}