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
    [SerializeField] private TMP_Text renderScaleValueText; // Visual percentage readout (e.g., "100%")

    private void Awake()
    {
        if (settingsManager == null)
        {
            settingsManager = FindFirstObjectByType<GraphicsSettingsManager>();
        }
    }

    private void OnEnable()
    {
        // Re-bind and refresh UI values whenever the settings panel opens
        SetupQualityDropdown();
        SetupResolutionDropdown();
        SetupTogglesAndSliders();
    }

    private void OnDisable()
    {
        RemoveAllListeners();
    }

    private void OnDestroy()
    {
        RemoveAllListeners();
    }

    #region --- Setup Methods ---
    private void SetupQualityDropdown()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.onValueChanged.RemoveAllListeners();
        qualityDropdown.ClearOptions();

        List<string> options = new List<string>(QualitySettings.names);
        qualityDropdown.AddOptions(options);

        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();

        qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null || settingsManager == null) return;

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.ClearOptions();

        List<Resolution> resolutions = settingsManager.GetFilteredResolutions();
        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Count; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height}";
            options.Add(option);
        }

        resolutionDropdown.AddOptions(options);

        resolutionDropdown.value = settingsManager.GetCurrentResolutionIndex();
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void SetupTogglesAndSliders()
    {
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        if (vsyncToggle != null)
        {
            vsyncToggle.onValueChanged.RemoveAllListeners();
            vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
            vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        }

        if (renderScaleSlider != null)
        {
            renderScaleSlider.onValueChanged.RemoveAllListeners();
            renderScaleSlider.minValue = 0.5f;
            renderScaleSlider.maxValue = 1.5f;

            float savedScale = PlayerPrefs.GetFloat("RenderScale", 1.0f);
            renderScaleSlider.value = savedScale;
            UpdateRenderScaleText(savedScale);

            renderScaleSlider.onValueChanged.AddListener(OnRenderScaleChanged);
        }
    }

    private void RemoveAllListeners()
    {
        if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveAllListeners();
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveAllListeners();
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveAllListeners();
        if (vsyncToggle != null) vsyncToggle.onValueChanged.RemoveAllListeners();
        if (renderScaleSlider != null) renderScaleSlider.onValueChanged.RemoveAllListeners();
    }
    #endregion

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
        UpdateRenderScaleText(value);
    }

    private void UpdateRenderScaleText(float value)
    {
        if (renderScaleValueText != null)
        {
            // Displays as percentage: e.g. "100%" or "125%"
            renderScaleValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }
    #endregion
}
