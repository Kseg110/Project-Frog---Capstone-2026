using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private GameObject controlsPanel;

    private GameObject currentPanel;

    private void Start()
    {
        ShowPanel(mainMenuPanel);
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel"))
            {
                OnBackClicked();
            }
        }
    }

    // ---------------- PANEL MANAGEMENT ----------------

    public void ShowPanel(GameObject panel)
    {
        if (currentPanel != null)
            currentPanel.SetActive(false);

        currentPanel = panel;
        currentPanel.SetActive(true);
    }

    public void OnBackClicked()
    {
        if (currentPanel == audioPanel || currentPanel == videoPanel || currentPanel == controlsPanel)
        {
            ShowPanel(optionsPanel);
        }
        else if (currentPanel == optionsPanel)
        {
            ShowPanel(mainMenuPanel);
        }
        else
        {
            ShowPanel(mainMenuPanel);
        }
    }

    // ---------------- PRIMARY BUTTONS ----------------

    public void OnStartClicked()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnOptionsClicked()
    {
        ShowPanel(optionsPanel);
    }

    public void OnExitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnCreditsClicked()
    {
        SceneManager.LoadScene("Credits");
    }

    // ---------------- SUB-MENUS ----------------

    public void OnAudioClicked()
    {
        ShowPanel(audioPanel);
    }

    public void OnVideoClicked()
    {
        ShowPanel(videoPanel);
    }

    public void OnControlsClicked()
    {
        ShowPanel(controlsPanel);
    }
}