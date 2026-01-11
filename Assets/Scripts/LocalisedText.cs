using TMPro;
using UnityEngine;

public class LocalizedText : MonoBehaviour
{
    public string key; // ex: "start_button"
    private TextMeshProUGUI textMesh;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        UpdateText();
    }

    public void UpdateText()
    {
        textMesh.text = LanguageManager.GetTranslation(key);
    }
}