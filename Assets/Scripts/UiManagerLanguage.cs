using UnityEngine;

public class UIManagerLanguage : MonoBehaviour
{
    // fonction to set language
    public void SetLanguageFrench()
    {
        LanguageManager.CurrentLanguage = "fr";
        RefreshUITexts();
    }

    public void SetLanguageEnglish()
    {
        LanguageManager.CurrentLanguage = "en";
        RefreshUITexts();
    }

    public void SetLanguageSpanish()
    {
        LanguageManager.CurrentLanguage = "es";
        RefreshUITexts();
    }

    public void SetLanguageChinese()
    {
        LanguageManager.CurrentLanguage = "zh";
        RefreshUITexts();
    }

    // force refresh all LocalizedText components in the scene
    private void RefreshUITexts()
    {
        // find all LocalizedText components and call UpdateText on each
        LocalizedText[] texts = Object.FindObjectsByType<LocalizedText>(FindObjectsSortMode.None);
        foreach (var t in texts)
        {
            t.UpdateText();
        }
    }
}
