using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(LineRenderer))]
public class AnchorRadiusVFX : MonoBehaviour
{
    public enum AnchorColorType { Red, Green, Blue, Custom }

    [Header("Visual Transform Settings")]
    [SerializeField] private Transform ringVisualTransform;
    [SerializeField] private DecalProjector decalProjector;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Line Renderer Settings")]
    [SerializeField] private bool useLineRenderer = true;
    [SerializeField] private int segments = 64;
    [SerializeField] private float lineWidth = 0.15f;

    [Header("Color & Glow Settings")]
    [SerializeField] private AnchorColorType colorType = AnchorColorType.Green;
    [SerializeField] private Color customColor = Color.green;
    [SerializeField] private float glowIntensity = 2.5f; // Multiplier for HDR Bloom Glow

    [Header("Fade Timings")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    private LineRenderer lineRenderer;
    private Coroutine fadeCoroutine;
    private Material lineMaterial;
    private Color activeBaseColor;

    private void Awake()
    {
        if (ringVisualTransform == null)
            ringVisualTransform = transform;

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.enabled = useLineRenderer;
            if (useLineRenderer)
            {
                lineRenderer.useWorldSpace = false;
                lineRenderer.loop = true;
                lineRenderer.positionCount = segments;
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;

                if (lineRenderer.material != null)
                {
                    lineMaterial = new Material(lineRenderer.material);
                    lineRenderer.material = lineMaterial;
                }
            }
        }

        activeBaseColor = GetColorFromType(colorType);
        SetAlpha(0f);
    }

    /// <summary>
    /// Displays ring visual, updates radius, sets anchor color/glow, and triggers smooth fade-in.
    /// </summary>
    public void ShowRadius(float radius, Color? newColor = null)
    {
        if (newColor.HasValue)
        {
            activeBaseColor = newColor.Value;
        }
        else
        {
            activeBaseColor = GetColorFromType(colorType);
        }

        if (ringVisualTransform != null)
        {
            ringVisualTransform.localScale = new Vector3(radius * 2f, ringVisualTransform.localScale.y, radius * 2f);
        }

        if (useLineRenderer && lineRenderer != null)
        {
            lineRenderer.enabled = true;
            DrawCirclePoints(radius);
        }

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(1f, fadeInDuration));
    }

    /// <summary>
    /// Smoothly fades out the ring visual.
    /// </summary>
    public void HideRadius()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(0f, fadeOutDuration));
    }

    /// <summary>
    /// Dynamically change the anchor color type at runtime.
    /// </summary>
    public void SetAnchorColor(AnchorColorType type)
    {
        colorType = type;
        activeBaseColor = GetColorFromType(type);
    }

    private Color GetColorFromType(AnchorColorType type)
    {
        switch (type)
        {
            case AnchorColorType.Red: return new Color(1f, 0.1f, 0.1f, 1f);
            case AnchorColorType.Green: return new Color(0.1f, 1f, 0.3f, 1f);
            case AnchorColorType.Blue: return new Color(0.1f, 0.5f, 1f, 1f);
            default: return customColor;
        }
    }

    private void DrawCirclePoints(float radius)
    {
        float angleStep = 2f * Mathf.PI / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            lineRenderer.SetPosition(i, new Vector3(x, 0.05f, z));
        }
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = GetAlpha();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;

            // Smoothstep curve for seamless fading
            float smoothProgress = Mathf.SmoothStep(0f, 1f, normalizedTime);
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, smoothProgress);

            SetAlpha(currentAlpha);
            yield return null;
        }

        SetAlpha(targetAlpha);

        if (targetAlpha <= 0.01f && useLineRenderer && lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void SetAlpha(float alpha)
    {
        // Calculate HDR Glow Color
        Color hdrGlowColor = activeBaseColor * glowIntensity;
        hdrGlowColor.a = alpha;

        Color alphaBaseColor = activeBaseColor;
        alphaBaseColor.a = alpha;

        // 1. Decal Fade & Color
        if (decalProjector != null)
        {
            decalProjector.fadeFactor = alpha;
        }

        // 2. Sprite Renderer Fade & Color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = alphaBaseColor;
        }

        // 3. Line Renderer Fade & Glow
        if (useLineRenderer && lineRenderer != null)
        {
            lineRenderer.startColor = hdrGlowColor;
            lineRenderer.endColor = hdrGlowColor;

            if (lineMaterial != null)
            {
                if (lineMaterial.HasProperty("_Color"))
                    lineMaterial.SetColor("_Color", alphaBaseColor);

                if (lineMaterial.HasProperty("_EmissionColor"))
                {
                    lineMaterial.SetColor("_EmissionColor", activeBaseColor * glowIntensity * alpha);
                    lineMaterial.EnableKeyword("_EMISSION");
                }
            }
        }
    }

    private float GetAlpha()
    {
        if (useLineRenderer && lineRenderer != null) return lineRenderer.startColor.a;
        if (decalProjector != null) return decalProjector.fadeFactor;
        if (spriteRenderer != null) return spriteRenderer.color.a;
        return 0f;
    }
}