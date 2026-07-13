using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;

public class CreditManager : MonoBehaviour
{
    [Header("Credit Data")]
    public CreditSection[] sections;

    [Header("UI")]
    public TextMeshProUGUI creditText;
    public float lineSpacing = 40f;

    [Header("Scroll Settings")]
    public RectTransform scrollRoot;
    public float scrollSpeed = 50f;

    void Start()
    {
        Time.timeScale = 1f;
        GenerateCredits();
    }

    void Update()
    {
        scrollRoot.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
    }

    void GenerateCredits()
    {
        creditText.text = "";

        foreach (var section in sections)
        {
            // Title
            creditText.text += $"<size=60><b>{section.title}</b></size>\n";

            // Names
            foreach (var name in section.names)
            {
                creditText.text += $"<size=40>{name}</size>\n";
            }

            // spacing between sections
            creditText.text += "\n";
        }
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}