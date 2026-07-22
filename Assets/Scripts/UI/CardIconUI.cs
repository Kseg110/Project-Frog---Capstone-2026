using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardIconUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI levelText;

    public void Setup(Sprite sprite, int level)
    {
        icon.sprite = sprite;
        levelText.text = "x" + level;
    }
}