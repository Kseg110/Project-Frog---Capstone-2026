using System.Collections;
using UnityEngine;

// Fades all renderers on this object to fully invisible over `duration`, then destroys it.
// REQUIRES the material's Surface Type = Transparent. Fades BOTH base color alpha AND emission — Disables colliders on fade start so a dying enemy doesn't keep blocking movement or registering on the tether. -E.M

public class EnemyFadeOut : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private float duration = 1.0f;
    [Tooltip("Left empty = auto-filled from all child renderers on Awake.")]
    [SerializeField] private Renderer[] renderers;

    [Header("Disable On Fade")]
    [Tooltip("Left empty = auto-filled from all child colliders on Awake.")]
    [SerializeField] private Collider[] collidersToDisable;

    // URP Lit uses _BaseColor; some shaders (or Built-in) use _Color. Resolve per-material.
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    // Original emission colors, captured at fade start so we scale from the true value rather than compounding frame to frame.
    private Color[] baseEmission;

    private bool isFading;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        if (collidersToDisable == null || collidersToDisable.Length == 0)
            collidersToDisable = GetComponentsInChildren<Collider>();
    }

    public void BeginFade()
    {
        if (isFading) return;   // guard against double-trigger
        isFading = true;

        // Stop the dying enemy from blocking movement or registering on the tether.
        foreach (var c in collidersToDisable)
            if (c != null) c.enabled = false;

        CaptureEmission();

        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    // Snapshot each renderer's starting emission color once, so ApplyAlpha can scale from the original toward black instead of reading an already-dimmed value each frame.
    private void CaptureEmission()
    {
        baseEmission = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r != null && r.material.HasProperty(EmissionColorId))
                baseEmission[i] = r.material.GetColor(EmissionColorId);
        }
    }

    private IEnumerator FadeRoutine()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - t / duration);
            ApplyAlpha(alpha);
            yield return null;
        }

        ApplyAlpha(0f);

        // Safety net: base-color alpha and emission are both zeroed, but hard-disable renderers so any residual specular/reflection is gone before destroy.
        foreach (var r in renderers)
            if (r != null) r.enabled = false;

        Destroy(gameObject);
    }

    private void ApplyAlpha(float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            // Fade base color alpha.
            int propId;
            if (r.material.HasProperty(BaseColorId)) propId = BaseColorId;
            else if (r.material.HasProperty(ColorId)) propId = ColorId;
            else propId = 0;

            if (propId != 0)
            {
                Color c = r.material.GetColor(propId);
                c.a = alpha;
                r.material.SetColor(propId, c);
            }

            // Fade emission toward black by the same factor so the glow dies with the surface.
            if (baseEmission != null && r.material.HasProperty(EmissionColorId))
                r.material.SetColor(EmissionColorId, baseEmission[i] * alpha);
        }
    }
}