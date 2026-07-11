using TMPro;
using UnityEngine;

public class CardIconManager : MonoBehaviour
{
    public Transform container; // GridLayoutGroup
    public GameObject iconPrefab;
    public TextMeshProUGUI descriptionText;

    public void RefreshIcons()
    {
        // Clear old icons
        foreach (Transform child in container)
            Destroy(child.gameObject);

        // Loop through all cards
        foreach (var entry in UpgradeManager.Instance.GetAllCards())
        {
            int level = UpgradeManager.Instance.GetLevel(entry);

            if (level <= 0)
                continue; // skip cards never chosen

            GameObject iconGO = Instantiate(iconPrefab, container);

            CardIconUI ui = iconGO.GetComponent<CardIconUI>();
            ui.descriptionText = descriptionText;
            ui.Setup(entry, level);
        }
    }
}