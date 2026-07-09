using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CardIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image icon;
    public TextMeshProUGUI levelText;

    [Header("Description UI")]
    public TextMeshProUGUI descriptionText;

    private UpgradeDataSO data;
    private int level;

    private Vector3 originalScale;

    public void Setup(UpgradeDataSO upgradeData, int lvl)
    {
        data = upgradeData;
        level = lvl;

        icon.sprite = upgradeData.Icon;
        levelText.text = "x" + lvl;

        originalScale = transform.localScale;

        if (descriptionText != null)
            descriptionText.text = "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Double size
        transform.localScale = originalScale * 2f;

        // Show current description
        if (descriptionText != null)
            descriptionText.text = data.GetCurrentDescription(level);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Reset size
        transform.localScale = originalScale;

        // Clear description
        if (descriptionText != null)
            descriptionText.text = "";
    }
}