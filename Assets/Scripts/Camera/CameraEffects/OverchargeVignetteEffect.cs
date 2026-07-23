using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Camera vignette effect that visualizes overcharge progress and cooldown.
// Vignette to match anchor type color.
// Intensity increase and decrease intensity during charging and cooldown.
public class OverchargeVignetteEffect : MonoBehaviour
{
    [Header("Vignette Colors")]
    [SerializeField] private Color fireColor = new Color (1f, 0.3f, 0f, 1f);
    [SerializeField] private Color iceColor = new Color (0f, 0.7f, 1f, 1f);
    [SerializeField] private Color windColor = new Color (0f, 1f, 0.3f, 1f);
    [SerializeField] private Color defaultColor = Color.white;

    [Header("Vignette Settings")]
    [SerializeField] private float baseIntensity = 0f;
    [SerializeField] private float maxOverchargeIntensity = 0.5f;
    [SerializeField] private float lerpSpeed = 5f;
    [SerializeField] private float smoothness = 0.4f;

    [Header("References")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private PlayerOvercharge playerOvercharge;
    [SerializeField] private PlayerAnchor playerAnchor;

    private Vignette vignette;
    private float targetIntensity = 0f;
    private Color currentColor = Color.white;
    private Color targetColor = Color.white;

    private void Awake()
    {
        if (postProcessVolume == null)
        {
            postProcessVolume = GetComponent<Volume>();
        }

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            if (!postProcessVolume.profile.TryGet(out vignette))
            {
                vignette = postProcessVolume.profile.Add<Vignette>(false);
            }
        }

        if (playerOvercharge == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerOvercharge = player.GetComponent<PlayerOvercharge>();
            }
        }

        if (playerAnchor == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerAnchor = player.GetComponent<PlayerAnchor>();
            }
        }

        InitializeVignette();
    }

    private void OnEnable()
    {
        if (playerAnchor != null)
        {
            playerAnchor.OnAnchorChanged += HandleAnchorChanged;
        }
        
        if (playerOvercharge != null)
        {
            playerOvercharge.OnOverchargeActivated += HandleOverchargeActivated;
        }
    }

    private void OnDisable()
    {
        if (playerAnchor != null)
        {
            playerAnchor.OnAnchorChanged -= HandleAnchorChanged;
        }

        if (playerOvercharge != null)
        {
            playerOvercharge.OnOverchargeActivated -= HandleOverchargeActivated;
        }
    }

    private void InitializeVignette()
    {
        if (vignette != null)
        {
            vignette.active = true;
            vignette.intensity.value = baseIntensity;
            vignette.smoothness.value = smoothness;
            vignette.color.value = defaultColor;
            currentColor = defaultColor;
            targetColor = defaultColor;
        }
    }

    private void Update()
    {
        if (vignette == null || playerOvercharge == null)
            return;

        // Update target intensity on overcharge state
        if (playerOvercharge.IsInCooldown)
        {
            // During cooldown; reverse intensity with charge progress
            targetIntensity = Mathf.Lerp(maxOverchargeIntensity, baseIntensity, 1f - playerOvercharge.CooldownProgress);
        }
        else if (playerAnchor != null && playerAnchor.IsTethered)
        {
            targetIntensity = Mathf.Lerp(baseIntensity, maxOverchargeIntensity, playerOvercharge.ChargeProgress);
        }
        else
        {
            // Not Tethered and not in cooldown
            targetIntensity = baseIntensity;
        }

        // Smooth lerp intensity & color
        vignette.intensity.value = Mathf.Lerp(
            vignette.intensity.value,
            targetIntensity,
            Time.deltaTime * lerpSpeed
            );

        currentColor = Color.Lerp(
            currentColor,
            targetColor,
            Time.deltaTime * lerpSpeed
            );

        vignette.color.value = currentColor;
    }

    private void HandleAnchorChanged(AnchorBase anchor)
    {
        if (anchor == null)
        {
            targetColor = defaultColor;
            return;
        }

        // Set Color based on anchor type
        if (anchor is AnchorFire)
        {
            targetColor = fireColor;
        }
        else if (anchor is AnchorIce)
        {
            targetColor = iceColor;
        }
        else if (anchor is AnchorWind)
        {
            targetColor = windColor;
        }
        else
        {
            targetColor = defaultColor;
        }
    }

    private void HandleOverchargeActivated()
    {
        // Maintain color during cooldown 
        // Intensity will auto reverse during cooldown update()

        // FUTURE IMPLEMENTATION HOOK //

        // If a burst of short intensity is desired
        // vignette.intensity.value = maxOverchargeIntensity * 1.2f;

        // Add audio event can be handled here but can also fit better in the PlayerOvercharge script itself.
    }

    public void SetVignetteEnabled(bool enabled)
    {
        if (vignette != null)
        {
            vignette.active = enabled;
        }
    }

    public void ResetVignette()
    {
        targetIntensity = baseIntensity;
        targetColor = defaultColor;

        if (vignette != null)
        {
            vignette.intensity.value = baseIntensity;
            vignette.color.value = defaultColor;
        }
    }

}
