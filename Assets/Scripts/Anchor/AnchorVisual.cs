using UnityEngine;

public class AnchorVisual : MonoBehaviour
{
    [Header("Meshes To Activate Based On Upgrade Level")]
    [SerializeField] private GameObject meshLevel5;
    [SerializeField] private GameObject meshLevel10;

    [Header("Gold Material Settings")]
    [SerializeField] private Material goldMaterial;
    [SerializeField] private Renderer[] PIMPMyAnchor;

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
        }
    }

    private void UpdateVisuals()
    {
        if (anchor == null || UpgradeManager.Instance == null)
            return;    

        int totalUpgrades = GetUpgradeCountForElement(anchor.Element);

        meshLevel5?.SetActive(totalUpgrades >= 5);
        meshLevel10?.SetActive(totalUpgrades >= 10);

        if (totalUpgrades >= 15 && goldMaterial != null)
        {
            foreach (var r in PIMPMyAnchor)
            {
                if (r != null)
                    r.material = goldMaterial;
            }
        }
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