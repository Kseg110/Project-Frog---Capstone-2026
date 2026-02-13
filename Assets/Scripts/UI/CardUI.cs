using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image backgroundFrame;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button button;

    [Header("Frame Sprites")]
    [SerializeField] private Sprite fireCard;
    [SerializeField] private Sprite iceCard;
    [SerializeField] private Sprite windCard;

    private CardData cardData;
    private System.Action<CardData> onSelected;

    public void Setup(CardData data, System.Action<CardData> callback)
    {
        cardData = data;
        onSelected = callback;

        // Set visuals
        icon.sprite = data.icon;
        title.text = data.cardName;
        description.text = data.description;

        // Set level text
        if (data.IsMaxed)
            levelText.text = "MAX";
        else
            levelText.text = "Lvl " + (data.currentLevel + 1);

        // Set frame based on element
        switch (data.element)
        {
            case AnchorElement.Fire:
                backgroundFrame.sprite = fireCard;
                break;
            case AnchorElement.Ice:
                backgroundFrame.sprite = iceCard;
                break;
            case AnchorElement.Wind:
                backgroundFrame.sprite = windCard;
                break;
        }

        // Set button callback
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected(cardData));
    }
}
