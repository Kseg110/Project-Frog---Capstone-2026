using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Primary Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button creditsButton;

    [Header("Sub-Menus Options")]
    [SerializeField] private Button audioButton;
    [SerializeField] private Button videoButton;
    [SerializeField] private Button controlsButton;

    [Header("Sub-Menus Audio")]
    [SerializeField] private GameObject audioPanel; // Parent container for audio UI
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxLabel;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TextMeshProUGUI musicLabel;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private TextMeshProUGUI masterLabel;

    [Header("Sub-Menus Video")]
    [SerializeField] private GameObject videoPanel; // Parent container or panel for video UI
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private Slider renderScaleSlider;

    [Header("Sub-Menus Video Labels")]
    [SerializeField] private TextMeshProUGUI qualityLabel;
    [SerializeField] private TextMeshProUGUI resolutionLabel;
    [SerializeField] private TextMeshProUGUI renderScaleLabel;
    [SerializeField] private TextMeshProUGUI renderScaleValueText;

    [Header("Sub-Menus Controls")]
    [SerializeField] private GameObject controlsPanel; // Parent container for controls UI
    [SerializeField] private GameObject keyboardImage;
    [SerializeField] private GameObject controllerImage;

    private bool isOptionsExpanded;
    private bool isAudioButtonOpen;
    private bool isVideoButtonOpen;
    private bool isControlsButtonOpen;

    private void Start()
    {
        // Hide sub-menu navigation buttons initially
        SetSubMenuButtonsActive(false);

        // Hide all sub-panels on start
        CloseAllSubMenus();
    }

    #region --- Primary Menu Actions ---
    public void OnStartClicked()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnOptionsClicked()
    {
        isOptionsExpanded = !isOptionsExpanded;
        SetSubMenuButtonsActive(isOptionsExpanded);

        if (!isOptionsExpanded)
        {
            CloseAllSubMenus();
        }
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
    #endregion

    #region --- Sub-Menu Navigation ---
    public void OnVideoClicked()
    {
        if (!isVideoButtonOpen)
        {
            CloseAllSubMenus();
            isVideoButtonOpen = true;

            if (videoPanel != null) videoPanel.SetActive(true);
            SetVideoElementsActive(true);

            videoButton.interactable = false;
        }
    }

    public void OnAudioClicked()
    {
        if (!isAudioButtonOpen)
        {
            CloseAllSubMenus();
            isAudioButtonOpen = true;

            if (audioPanel != null) audioPanel.SetActive(true);
            SetAudioElementsActive(true);

            audioButton.interactable = false;
        }
    }

    public void OnControlsClicked()
    {
        if (!isControlsButtonOpen)
        {
            CloseAllSubMenus();
            isControlsButtonOpen = true;

            if (controlsPanel != null) controlsPanel.SetActive(true);
            keyboardImage.SetActive(true);
            controllerImage.SetActive(true);

            controlsButton.interactable = false;
        }
    }

    private void CloseAllSubMenus()
    {
        // Close Audio
        isAudioButtonOpen = false;
        if (audioPanel != null) audioPanel.SetActive(false);
        SetAudioElementsActive(false);

        // Close Video
        isVideoButtonOpen = false;
        if (videoPanel != null) videoPanel.SetActive(false);
        SetVideoElementsActive(false);

        // Close Controls
        isControlsButtonOpen = false;
        if (controlsPanel != null) controlsPanel.SetActive(false);
        keyboardImage.SetActive(false);
        controllerImage.SetActive(false);

        // Re-enable navigation buttons
        if (audioButton != null) audioButton.interactable = true;
        if (videoButton != null) videoButton.interactable = true;
        if (controlsButton != null) controlsButton.interactable = true;
    }

    private void SetSubMenuButtonsActive(bool active)
    {
        if (audioButton != null) audioButton.gameObject.SetActive(active);
        if (videoButton != null) videoButton.gameObject.SetActive(active);
        if (controlsButton != null) controlsButton.gameObject.SetActive(active);
    }

    private void SetAudioElementsActive(bool active)
    {
        if (sfxSlider != null) sfxSlider.gameObject.SetActive(active);
        if (sfxLabel != null) sfxLabel.gameObject.SetActive(active);
        if (musicSlider != null) musicSlider.gameObject.SetActive(active);
        if (musicLabel != null) musicLabel.gameObject.SetActive(active);
        if (masterSlider != null) masterSlider.gameObject.SetActive(active);
        if (masterLabel != null) masterLabel.gameObject.SetActive(active);
    }

    private void SetVideoElementsActive(bool active)
    {
        if (resolutionDropdown != null) resolutionDropdown.gameObject.SetActive(active);
        if (qualityDropdown != null) qualityDropdown.gameObject.SetActive(active);
        if (fullscreenToggle != null) fullscreenToggle.gameObject.SetActive(active);
        if (vsyncToggle != null) vsyncToggle.gameObject.SetActive(active);
        if (renderScaleSlider != null) renderScaleSlider.gameObject.SetActive(active);

        // Disable text labels:
        if (qualityLabel != null) qualityLabel.gameObject.SetActive(active);
        if (resolutionLabel != null) resolutionLabel.gameObject.SetActive(active);
        if (renderScaleLabel != null) renderScaleLabel.gameObject.SetActive(active);
        if (renderScaleValueText != null) renderScaleValueText.gameObject.SetActive(active);
    }
    #endregion
}