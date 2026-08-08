using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GraphicsSettingsManager : MonoBehaviour
{
    private List<Resolution> filteredResolutions = new List<Resolution>();
    private int currentResolutionIndex = 0;

    private void Start()
    {
        InitializeResolutions();
        LoadSettings();
    }

    #region --- Quality Presets ---
    public void SetQualityLevel(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex, applyExpensiveChanges: true);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
        PlayerPrefs.Save();
    }
    #endregion

    #region --- Display & Resolution Settings ---
    private void InitializeResolutions()
    {
        Resolution[] allResolutions = Screen.resolutions;
        filteredResolutions.Clear();

        double currentRefreshRate = Screen.currentResolution.refreshRateRatio.value;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            // Filter out duplicate width x height entries and keep match for current refresh rate
            if (Mathf.Approximately((float)allResolutions[i].refreshRateRatio.value, (float)currentRefreshRate))
            {
                filteredResolutions.Add(allResolutions[i]);

                if (allResolutions[i].width == Screen.width &&
                    allResolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = filteredResolutions.Count - 1;
                }
            }
        }
    }

    public List<Resolution> GetFilteredResolutions()
    {
        return filteredResolutions;
    }

    public int GetCurrentResolutionIndex()
    {
        return currentResolutionIndex;
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex >= 0 && resolutionIndex < filteredResolutions.Count)
        {
            Resolution res = filteredResolutions[resolutionIndex];
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
            
            PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
            PlayerPrefs.Save();
        }
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
        PlayerPrefs.SetInt("Fullscreen", isFullScreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetVSync(bool isEnabled)
    {
        // 0 = Off, 1 = On (60 FPS / Monitor Hz)
        QualitySettings.vSyncCount = isEnabled ? 1 : 0;
        PlayerPrefs.SetInt("VSync", isEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetTargetFPS(int targetFPS)
    {
        if (QualitySettings.vSyncCount == 0)
        {
            Application.targetFrameRate = targetFPS;
            PlayerPrefs.SetInt("TargetFPS", targetFPS);
            PlayerPrefs.Save();
        }
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
            PlayerPrefs.Save();
        }
    }

    public void SetMainCameraAntiAliasing(Camera targetCamera, AntialiasingMode mode, AntialiasingQuality quality)
    {
        if (targetCamera == null) targetCamera = Camera.main;

        if (targetCamera != null && targetCamera.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData))
        {
            cameraData.antialiasing = mode;
            cameraData.antialiasingQuality = quality;
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

        if (PlayerPrefs.HasKey("RenderScale"))
            SetRenderScale(PlayerPrefs.GetFloat("RenderScale"));

        if (PlayerPrefs.HasKey("TargetFPS"))
            SetTargetFPS(PlayerPrefs.GetInt("TargetFPS"));
    }
    #endregion
}
