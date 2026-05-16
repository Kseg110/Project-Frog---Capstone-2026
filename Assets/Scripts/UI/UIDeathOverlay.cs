using UnityEngine;
using UnityEngine.SceneManagement;

public class UIDeathOverlay : MonoBehaviour
{
    // The UI panel that appears when the player dies
    public GameObject deathOverlayPanel;
    public string mainMenuSceneName = "MainMenu";

    private bool isDeathOverlayOpen;

    private void Start()
    {
        Time.timeScale = 1f;
        // Tracks whether the death overlay is currently open.
        isDeathOverlayOpen = false;

        if (deathOverlayPanel != null)
            deathOverlayPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isDeathOverlayOpen) return;

        if (Input.GetKeyDown(KeyCode.R))
            RestartScene();

        if (Input.GetKeyDown(KeyCode.M))
            ReturnToMainMenu();
    }

    // This function is called when the player dies.
    public void ShowDeathOverlay()
    {
        if (deathOverlayPanel != null)
            deathOverlayPanel.SetActive(true);

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