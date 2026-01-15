using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Sub-Menus Options")]
    [SerializeField] private GameObject audioMenu;
    [SerializeField] private GameObject videoMenu;
    [SerializeField] private GameObject controlsMenu;

    [Header("Primary Buttons")]
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject optionButton;
    [SerializeField] private GameObject exitButton;
    [SerializeField] private GameObject creditsButton;

    [Header("Sub-Menus Audio")]
    [SerializeField] private GameObject sfxSlider;
    [SerializeField] private GameObject sfxLabel;
    [SerializeField] private GameObject musicSlider;
    [SerializeField] private GameObject musicLabel;
    [SerializeField] private GameObject masterSlider;
    [SerializeField] private GameObject masterLabel;

    [Header("Sub-Menus Video")]


    [Header("Sub-Menus Controls")]
    [SerializeField] private GameObject keyboardImage;
    [SerializeField] private GameObject controllerImage;


    private bool isOptionsExpanded;
    private bool isAudioMenuOpen;
    private bool isVideoMenuOpen;
    private bool isControlsOpen;

    private void Start()
    {
        audioMenu.SetActive(false);
        videoMenu.SetActive(false);
        controlsMenu.SetActive(false);
        sfxSlider.SetActive(false);
        sfxLabel.SetActive(false);
        musicSlider.SetActive(false);
        musicLabel.SetActive(false);
        masterSlider.SetActive(false);
        masterLabel.SetActive(false);
        keyboardImage.SetActive(false);
        controllerImage.SetActive(false);
    }

    // --- call primary fonction ---
    public void OnStartClicked()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnOptionsClicked()
    {
        // open/close settings menu
        isOptionsExpanded = !isOptionsExpanded;
        
        audioMenu.SetActive(isOptionsExpanded);
        videoMenu.SetActive(isOptionsExpanded);
        controlsMenu.SetActive(isOptionsExpanded);

        // Close audio sub-menu if options menu is closed
        if (!isOptionsExpanded)
        {
            isAudioMenuOpen = false;

            sfxSlider.SetActive(false);
            sfxLabel.SetActive(false);
            musicSlider.SetActive(false);
            musicLabel.SetActive(false);
            masterSlider.SetActive(false);
            masterLabel.SetActive(false);
        }

        // Close controls sub-menu if options menu is closed
        if (!isOptionsExpanded)
        {
            isControlsOpen = false;
            keyboardImage.SetActive(false);
            controllerImage.SetActive(false);
        }
    }

    public void OnExitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stops play mode in the editor
#endif
    }

    public void OnCreditsClicked()
    {
        SceneManager.LoadScene("Credits");
    }

    //Sub-menu functions

    public void OnVideoClicked()
    {
        Debug.Log("Menu Video open");
        // show video options
    }

    public void OnAudioClicked()
    {
        // open/close audio options
        isAudioMenuOpen = !isAudioMenuOpen;

        sfxSlider.SetActive(isAudioMenuOpen);
        sfxLabel.SetActive(isAudioMenuOpen);
        musicSlider.SetActive(isAudioMenuOpen);
        musicLabel.SetActive(isAudioMenuOpen);
    }

    public void OnControlsClicked()
    {
        isControlsOpen = !isControlsOpen;

        keyboardImage.SetActive(isControlsOpen);
        controllerImage.SetActive(isControlsOpen);
    }
}