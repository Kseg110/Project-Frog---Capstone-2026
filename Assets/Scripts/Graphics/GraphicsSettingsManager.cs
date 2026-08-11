using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GraphicsSettingsManager : MonoBehaviour
{
    private List<Resolution> filteredResolutions = new List<Resolution>();
    private int currentResolutionIndex = 0;

    private void Awake()
    {
        InitializeResolutions();
        LoadSettings();
    }

    #region --- Quality Presets ---
    public void SetQualityLevel(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex, applyExpensiveChanges: true);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
    }
    #endregion

    #region --- Display & Resolution Settings ---
    private void InitializeResolutions()
    {
        Resolution[] allResolutions = Screen.resolutions;
        filteredResolutions.Clear();

        // Unique resolution width x height mapping to keep the highest available refresh rate
        Dictionary<(int, int), Resolution> uniqueResolutions = new Dictionary<(int, int), Resolution>();

        foreach (var res in allResolutions)
        {
            var key = (res.width, res.height);
            if (!uniqueResolutions.ContainsKey(key) || res.refreshRateRatio.value > uniqueResolutions[key].refreshRateRatio.value)
            {
                uniqueResolutions[key] = res;
            }
        }

        filteredResolutions.AddRange(uniqueResolutions.Values);

        // Find current matching resolution index
        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            if (filteredResolutions[i].width == Screen.width &&
                filteredResolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
                break;
            }
        }
    }

    public List<Resolution> GetFilteredResolutions() => filteredResolutions;

    public int GetCurrentResolutionIndex() => currentResolutionIndex;

    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex >= 0 && resolutionIndex < filteredResolutions.Count)
        {
            Resolution res = filteredResolutions[resolutionIndex];
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
            currentResolutionIndex = resolutionIndex;

            PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        }
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
        PlayerPrefs.SetInt("Fullscreen", isFullScreen ? 1 : 0);
    }

    public void SetVSync(bool isEnabled)
    {
        QualitySettings.vSyncCount = isEnabled ? 1 : 0;
        PlayerPrefs.SetInt("VSync", isEnabled ? 1 : 0);
    }

    public void SetTargetFPS(int targetFPS)
    {
        Application.targetFrameRate = targetFPS;
        PlayerPrefs.SetInt("TargetFPS", targetFPS);
    }
    #endregion

    #region --- URP Specific Settings ---
    public void SetRenderScale(float scale)
    {
        scale = Mathf.Clamp(scale, 0.5f, 1.5f);

        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urpAsset)
        {
            urpAsset.renderScale = scale;
            PlayerPrefs.SetFloat("RenderScale", scale);
        }
    }

    public void SetMainCameraAntiAliasing(Camera targetCamera, AntialiasingMode mode, AntialiasingQuality quality)
    {
        if (targetCamera == null) targetCamera = Camera.main;

        if (targetCamera != null && targetCamera.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData))
        {
            cameraData.antialiasing = mode;
            cameraData.antialiasingQuality = quality;

            PlayerPrefs.SetInt("AAMode", (int)mode);
            PlayerPrefs.SetInt("AAQuality", (int)quality);
        }
    }
    #endregion

    #region --- Save & Load ---
    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey("QualityLevel"))
            SetQualityLevel(PlayerPrefs.GetInt("QualityLevel"));

        if (PlayerPrefs.HasKey("Fullscreen"))
            SetFullScreen(PlayerPrefs.GetInt("Fullscreen") == 1);

        if (PlayerPrefs.HasKey("VSync"))
            SetVSync(PlayerPrefs.GetInt("VSync") == 1);

        if (PlayerPrefs.HasKey("TargetFPS"))
            SetTargetFPS(PlayerPrefs.GetInt("TargetFPS"));

        if (PlayerPrefs.HasKey("RenderScale"))
            SetRenderScale(PlayerPrefs.GetFloat("RenderScale"));

        if (PlayerPrefs.HasKey("ResolutionIndex"))
            SetResolution(PlayerPrefs.GetInt("ResolutionIndex"));

        // Save once after applying all loaded prefs rather than on every setting write
        PlayerPrefs.Save();
    }
    #endregion
}
