using UnityEngine;
using UnityEngine.EventSystems;
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


    private void Start()
    {
        gameObject.SetActive(true);

        if (deathOverlayPanel != null)
            deathOverlayPanel.SetActive(false);

        Time.timeScale = 1f;

        // Set up button listeners
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


        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
        SelectDefaultButton();
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

    private void SelectDefaultButton()
    {
        var es = EventSystem.current;

        if (RestartButton != null)
            es.SetSelectedGameObject(RestartButton.gameObject);
        else if (MenuButton != null)
            es.SetSelectedGameObject(MenuButton.gameObject);
    }
}