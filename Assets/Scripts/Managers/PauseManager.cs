using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;

    private PlayerInput playerInput;
    private InputAction pauseAction;

    private string currentActionMapName;
    private bool isPaused = false;

    private void Awake()
    {
        playerInput = FindAnyObjectByType<PlayerInput>();
        RebindActionsFromCurrentMap();
    }

    private void OnEnable()
    {
        if (playerInput != null)
            playerInput.onControlsChanged += OnControlsChanged;
    }

    private void OnDisable()
    {
        if (playerInput != null)
            playerInput.onControlsChanged -= OnControlsChanged;
    }

    private void Update()
    {
        // Rebind if the action map has changed 
        if (playerInput != null &&
            playerInput.currentActionMap != null &&
            playerInput.currentActionMap.name != currentActionMapName)
        {
            RebindActionsFromCurrentMap();
        }

        if (pauseAction == null)
            return;

        // Look for the pause input
        if (pauseAction.WasPressedThisFrame())
        {
            TogglePause();
        }
    }

    private void OnControlsChanged(PlayerInput pi)
    {
        RebindActionsFromCurrentMap();
    }

    private void RebindActionsFromCurrentMap()
    {
        if (playerInput == null || playerInput.currentActionMap == null)
            return;

        currentActionMapName = playerInput.currentActionMap.name;
        pauseAction = playerInput.currentActionMap.FindAction("Pause");
    }

    private void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        StartCoroutine(ReenableGameplayInput());
    }

    private IEnumerator ReenableGameplayInput()
    {
        // Yeld for a frame to ensure the pause menu has fully deactivated and any input state has reset
        yield return null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void QuitGame()
    {
        SceneManager.LoadScene("MainMenu");
    }
}