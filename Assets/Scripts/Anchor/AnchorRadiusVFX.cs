using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AnchorRadiusVFX : MonoBehaviour
{
    [Header("Visual Components")]
    [Tooltip("Assign the child GameObject that holds the Decal, Sprite, or Ring Mesh.")]
    [SerializeField] private Transform ringVisualTransform;
    [SerializeField] private DecalProjector decalProjector;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Fade Timings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // Fallback to self if ringVisualTransform is not assigned in Inspector
        if (ringVisualTransform == null)
            ringVisualTransform = transform;

        SetAlpha(0f);
    }

    /// <summary>
    /// Displays and scales ONLY the visual ring to match the radius.
    /// </summary>
    public void ShowRadius(float radius)
    {
        // Scale diameter (Radius * 2) on X and Z axes of the child visual object only
        if (ringVisualTransform != null)
        {
            ringVisualTransform.localScale = new Vector3(radius * 2f, ringVisualTransform.localScale.y, radius * 2f);
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

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = GetAlpha();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            SetAlpha(currentAlpha);
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (decalProjector != null) decalProjector.fadeFactor = alpha;
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }

    private float GetAlpha()
    {
        if (decalProjector != null) return decalProjector.fadeFactor;
        if (spriteRenderer != null) return spriteRenderer.color.a;
        return 0f;
    }
}
