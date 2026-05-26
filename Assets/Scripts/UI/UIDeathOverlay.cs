using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIDeathOverlay : MonoBehaviour
{
    // The UI panel that appears when the player dies
    [SerializeField] private GameObject deathOverlayPanel;
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private Button RestartButton;
    [SerializeField] private Button MenuButton;
    private string mainMenuSceneName = "MainMenu";

    private bool isDeathOverlayOpen;

    private void Start()
    {
        Time.timeScale = 1f;
        // Tracks whether the death overlay is currently open.
        isDeathOverlayOpen = false;

        if (deathOverlayPanel != null)
            deathOverlayPanel.SetActive(false);

        if (RestartButton != null)
        {
            RestartButton.onClick.RemoveAllListeners();
            RestartButton.onClick.AddListener(RestartScene);
        }

        if (MenuButton != null)
        {
            MenuButton.onClick.RemoveAllListeners();
            MenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    private void OnDestroy()
    {
        if (RestartButton != null)
            RestartButton.onClick.RemoveListener(RestartScene);
        if (MenuButton != null)
            MenuButton.onClick.RemoveListener(ReturnToMainMenu);
    }

    // Function is called when the player dies.
    public void ShowDeathOverlay()
    {
        if (deathOverlayPanel != null)
            deathOverlayPanel.SetActive(true);
        if (playerHUD != null)
            playerHUD.SetActive(false);

        isDeathOverlayOpen = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;

        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}