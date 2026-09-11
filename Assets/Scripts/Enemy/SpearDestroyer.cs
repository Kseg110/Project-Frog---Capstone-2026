using UnityEngine;

public class SpearDestroyer : MonoBehaviour
{
    [Tooltip("Names of upgrades which, when acquired, will cause this spear to be destroyed.")]
    [SerializeField] private string[] destroyWhenUpgradesPresent = new string[] { "DisableSpears" };

    private void Start()
    {
        // Evaluate immediately in case the upgrade is already active
        EvaluateAndDestroy();

        // Subscribe to changes so spears present in the scene will react when the player chooses an upgrade
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradesChanged += OnUpgradesChanged;

        // Subscribe to card selection UI show event so spears are removed as soon as the upgrade UI appears
        CardSelectionUI.OnCardSelectionShown += OnCardSelectionShown;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradesChanged -= OnUpgradesChanged;

        CardSelectionUI.OnCardSelectionShown -= OnCardSelectionShown;
    }

    private void OnUpgradesChanged()
    {
        EvaluateAndDestroy();
    }

    private void OnCardSelectionShown()
    {
        // Destroy immediately when the card selection screen appears (safeguard for flying spears)
        Destroy(gameObject);
    }

    private void EvaluateAndDestroy()
    {
        if (destroyWhenUpgradesPresent == null || destroyWhenUpgradesPresent.Length == 0)
            return;

        var mgr = UpgradeManager.Instance;
        if (mgr == null)
            return;

        foreach (var name in destroyWhenUpgradesPresent)
        {
            if (string.IsNullOrEmpty(name)) continue;
            if (mgr.HasUpgrade(name))
            {
                // Immediately destroy the spear instance when the upgrade is present
                Destroy(gameObject);
                return;
            }
        }
    }
}
