using UnityEngine;

public class AnchorVisual : MonoBehaviour
{
    [Header("Meshes To Activate Based On Upgrade Level")]
    [SerializeField] private GameObject meshLevel5;
    [SerializeField] private GameObject meshLevel10;
    [SerializeField] private GameObject meshLevel15;

    private AnchorBase anchor;
    private bool isSubscribed = false;

    private void Awake()
    {
        anchor = GetComponent<AnchorBase>();
    }

    private void OnEnable()
    {
        TrySubscribe();
        UpdateVisuals();
    }

    private void Start()
    {
        TrySubscribe();
        UpdateVisuals();
    }

    private void OnDisable()
    {
        if (UpgradeManager.Instance != null && isSubscribed)
        {
            UpgradeManager.Instance.OnUpgradesChanged -= UpdateVisuals;
            isSubscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (UpgradeManager.Instance != null && !isSubscribed)
        {
            UpgradeManager.Instance.OnUpgradesChanged += UpdateVisuals;
            isSubscribed = true;
            Debug.Log($"[AnchorVisual] Subscribed for {anchor.Element} on {name}");
        }
    }

    private void UpdateVisuals()
    {
        if (anchor == null || UpgradeManager.Instance == null)
        {
            Debug.LogWarning($"[AnchorVisual] Missing anchor or UpgradeManager on {name}");
            return;
        }

        int totalUpgrades = GetUpgradeCountForElement(anchor.Element);
        Debug.Log($"[AnchorVisual] {anchor.Element} total upgrades = {totalUpgrades} on {name}");

        meshLevel5?.SetActive(totalUpgrades >= 5);
        meshLevel10?.SetActive(totalUpgrades >= 10);
        meshLevel15?.SetActive(totalUpgrades >= 15);
    }

    private int GetUpgradeCountForElement(AnchorElement element)
    {
        int count = 0;
        var cards = UpgradeManager.Instance.GetAllCards();

        foreach (var card in cards)
        {
            if (card.Element == element)
                count += UpgradeManager.Instance.GetLevel(card);
        }

        return count;
    }
}