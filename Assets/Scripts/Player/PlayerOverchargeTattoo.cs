using UnityEngine;

/// <summary>
/// This scripts logic is to control the player's emission tattoo.
/// It fills from bottom to top during the overcharges charging & drains from top to bottom during cooldown/ drain.
/// </summary>
public class PlayerOverchargeTattoo : MonoBehaviour
{
    [Header("Tattoo Settings")]
    [SerializeField] private Renderer playerRenderer;
    [SerializeField] private int materialIndex = 0;
    [SerializeField] private string fillPropertyName = "_FillAmount";
    [SerializeField] private string emissionColorPropertyName = "_EmissionColor";

    [Header("Anchor Colors")]
    [SerializeField] private Color fireColor = new Color(1f, 0.3f, 0f, 1f); // Orange Red
    [SerializeField] private Color iceColor = new Color(0f, 0.5f, 1f, 1f); // Cyan Blue
    [SerializeField] private Color windColor = new Color(0f, 1f, 0.3f, 1f); // Green
    [SerializeField] private Color neutralColor = new Color(1f, 1f, 1f, 1f); // White
    [SerializeField] private float emissionIntensity = 3f; // HDR Intensity


    [Header("References")]
    [SerializeField] private PlayerOvercharge playerOvercharge;
    [SerializeField] private PlayerAnchor playerAnchor;

    private Material tattooMaterial;
    private float currentFillAmount = 0f;
    private Color currentEmissionColor;
    private int fillPropertyID;
    private int emissionColorPropertyID;
    private AnchorBase currentAnchor;
    private AnchorElement cachedElement = AnchorElement.Broken;

    private void Awake()
    {
        if (playerRenderer == null)
        {
            playerRenderer = GetComponentInChildren<Renderer>();
        }

        if (playerOvercharge == null)
        {
            playerOvercharge = GetComponent<PlayerOvercharge>();
        }
        
        if (playerAnchor == null)
        {
            playerAnchor = GetComponent<PlayerAnchor>();
        }

        // Create material instance to avoid affecting all instances during charge/ color change.
        if (playerRenderer != null)
        {
            tattooMaterial = playerRenderer.materials[materialIndex];
            fillPropertyID = Shader.PropertyToID(fillPropertyName);
            emissionColorPropertyID = Shader.PropertyToID(emissionColorPropertyName);
        }

        // Initialize colors
        currentEmissionColor = neutralColor * emissionIntensity;

        // Subscribe to overcharge event
        if (playerOvercharge != null)
        {
            playerOvercharge.OnChargeChanged += HandleChargeChanged;
            playerOvercharge.OnOverchargeActivated += HandleOverchargeActivated;
            playerOvercharge.OnCooldownComplete += HandleCooldownComplete;
        }
        // Subscribe to Anchor changed event
        if (playerAnchor != null)
        {
            playerAnchor.OnAnchorChanged += HandleAnchorChanged;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (playerOvercharge != null)
        {
            playerOvercharge.OnChargeChanged -= HandleChargeChanged;
            playerOvercharge.OnOverchargeActivated -= HandleOverchargeActivated;
            playerOvercharge.OnCooldownComplete -= HandleCooldownComplete;
        }

        if (playerAnchor != null)
        {
            playerAnchor.OnAnchorChanged -= HandleAnchorChanged;
        }

        // Clean up material instance
        if (tattooMaterial != null)
        {
            Destroy(tattooMaterial);
        }
    }

    private void HandleChargeChanged(float normalizedValue)
    {
        // Update fill amount based on state
        if (playerOvercharge.IsInCooldown)
        {
            // During cooldown
            currentFillAmount = normalizedValue;
        }
        else
        {
            // During charging
            currentFillAmount = normalizedValue;
        }
    }

    private void HandleAnchorChanged(AnchorBase newAnchor)
    {
        // Store anchor ref
        currentAnchor = newAnchor;

        if (currentAnchor != null)
        {
            cachedElement = currentAnchor.Element; // stores current element type from anchor
        }
        else
        {
            // If detached outside of overcharge, reset to neutral
            if (!playerOvercharge.IsOvercharged && !playerOvercharge.IsInCooldown)
            {
                cachedElement = AnchorElement.Broken;
            }
        }

        // Updates color when anchor is changed
        UpdateColorForCurrentAnchor();
    }

    private void HandleOverchargeActivated()
    {
        currentFillAmount = 1f;
    }

    private void HandleCooldownComplete()
    {
        cachedElement = AnchorElement.Broken; // Reset to neutral - no element
        currentAnchor = null; // clears anchor ref
        currentFillAmount = 0f; // resets fill
        UpdateColorForCurrentAnchor();
    }

    private void UpdateColorForCurrentAnchor()
    {
        Color baseColor = neutralColor;
        AnchorElement elementToUse = cachedElement;

        // Use current anchor's element if not in overcharge/cooldown
        if (!playerOvercharge.IsOvercharged && !playerOvercharge.IsInCooldown && currentAnchor != null)
        {
            elementToUse = currentAnchor.Element;
        }

        // Determines color based on anchor element
        switch (elementToUse)
        {
            case AnchorElement.Fire:
                baseColor = fireColor;
                break;
            case AnchorElement.Ice:
                baseColor = iceColor;
                break;
            case AnchorElement.Wind:
                baseColor = windColor;
                break;
            default:
                baseColor = neutralColor;
                break;
        }

        // Apply intensity
        currentEmissionColor = baseColor * emissionIntensity;
    }

    private void Update()
    {
        if (tattooMaterial == null) return;

        // Update shader properties
        tattooMaterial.SetFloat(fillPropertyID, currentFillAmount);
        tattooMaterial.SetColor(emissionColorPropertyID, currentEmissionColor);
    }

}
