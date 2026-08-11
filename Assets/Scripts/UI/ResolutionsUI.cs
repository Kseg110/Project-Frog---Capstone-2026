using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class ResolutionsUI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private GraphicsSettingsManager graphicsManager;
    [SerializeField] private TMP_Dropdown resolutionDropDown;
    [SerializeField] private Toggle fullscreenToggle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (graphicsManager == null)
        {
            graphicsManager = FindAnyObjectByType<GraphicsSettingsManager>();
        }
            
        InitializeResolutionDropdown();
        InitializeFullscreenToggle();
    }

    private void InitializeResolutionDropdown()
    {
        if (resolutionDropDown == null) return;

        // Clear existing default options;
        resolutionDropDown.ClearOptions();

        List<Resolution> resolutions = graphicsManager.GetFilteredResolutions();
        List<string> options = new List<string>();

        // Build string labels for each resolution
        for (int i = 0; i < resolutions.Count; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height} @ {Mathf.Round((float)resolutions[i].refreshRateRatio.value)}Hz";
            options.Add(option);
        }

        // Populate dropdown options
        resolutionDropDown.AddOptions(options);

        // Select the currently active resolution index
        resolutionDropDown.value = graphicsManager.GetCurrentResolutionIndex();
        resolutionDropDown.RefreshShownValue();

        // Bind dropdown change event
        resolutionDropDown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void InitializeFullscreenToggle()
    {
        if (fullscreenToggle == null) return;

        fullscreenToggle.isOn = Screen.fullScreen;
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    // Called when the user picks an item in the dropdown
    public void OnResolutionChanged(int index)
    {
        graphicsManager.SetResolution(index);
    }

    // Called when the user clicks the fullscreen toggle
    public void OnFullscreenChanged(bool isFullscreen)
    {
        graphicsManager.SetFullScreen(isFullscreen);
    }

    private void OnDestroy()
    {
        // Clean up event listeners when object is destoryed
        if (resolutionDropDown != null)
        {
            resolutionDropDown.onValueChanged.RemoveListener(OnResolutionChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        }    
    }
}
