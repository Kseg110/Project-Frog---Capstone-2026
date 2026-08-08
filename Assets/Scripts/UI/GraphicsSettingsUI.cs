using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GraphicsSettingsUI : MonoBehaviour
{
    [Header("Manager Reference")]
    [SerializeField] private GraphicsSettingsManager settingsManager;

    [Header("UI Dropdowns")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("UI Toggles & Sliders")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private Slider renderScaleSlider;

    private void Start()
    {
        // Auto-locate manager if unassigned
        if (settingsManager == null)
        {
            settingsManager = FindFirstObjectByType<GraphicsSettingsManager>();
        }

        SetupQualityDropdown();
        SetupResolutionDropdown();
        SetupTogglesAndSliders();
    }

    private void SetupQualityDropdown()
    {
        if (qualityDropdown == null) return;

        // 1. Clear placeholder options
        qualityDropdown.ClearOptions();

        // 2. Fetch quality tier names defined in Project Settings
        List<string> options = new List<string>(QualitySettings.names);
        qualityDropdown.AddOptions(options);

        // 3. Set current value and refresh display
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();

        // 4. Register event listener
        qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null || settingsManager == null) return;

        // 1. Clear placeholder options
        resolutionDropdown.ClearOptions();

        // 2. Fetch deduplicated resolutions from manager
        List<Resolution> resolutions = settingsManager.GetFilteredResolutions();
        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Count; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height}";
            options.Add(option);
        }

        resolutionDropdown.AddOptions(options);

        // 3. Set current screen resolution index
        resolutionDropdown.value = settingsManager.GetCurrentResolutionIndex();
        resolutionDropdown.RefreshShownValue();

        // 4. Register event listener
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void SetupTogglesAndSliders()
    {
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
            vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        }

        if (renderScaleSlider != null)
        {
            renderScaleSlider.minValue = 0.5f;
            renderScaleSlider.maxValue = 1.5f;
            renderScaleSlider.value = PlayerPrefs.GetFloat("RenderScale", 1.0f);
            renderScaleSlider.onValueChanged.AddListener(OnRenderScaleChanged);
        }
    }

    #region --- UI Event Callbacks ---
    private void OnQualityChanged(int index)
    {
        settingsManager.SetQualityLevel(index);
    }

    private void OnResolutionChanged(int index)
    {
        settingsManager.SetResolution(index);
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        settingsManager.SetFullScreen(isFullscreen);
    }

    private void OnVSyncChanged(bool isVSync)
    {
        settingsManager.SetVSync(isVSync);
    }

    private void OnRenderScaleChanged(float value)
    {
        settingsManager.SetRenderScale(value);
    }
    #endregion
}
