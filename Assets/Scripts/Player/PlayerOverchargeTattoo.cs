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
    [SerializeField] private float lerpSpeed = 5f;

    [Header("References")]
    [SerializeField] private PlayerOvercharge playerOvercharge;

    private Material tattooMaterial;
    private float targetFillAmount = 0f;
    private float currentFillAmount = 0f;
    private int fillPropertyID;

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

        // Create material instance to avoid affecting all instances during charge/ color change.
        if (playerRenderer != null)
        {
            tattooMaterial = playerRenderer.materials[materialIndex];
            fillPropertyID = Shader.PropertyToID(fillPropertyName);
        }

        // Subscribe to overcharge event
        if (playerOvercharge != null)
        {
            playerOvercharge.OnChargeChanged += HandleChargeChanged;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (playerOvercharge != null)
        {
            playerOvercharge.OnChargeChanged -= HandleChargeChanged;
        }

        // Clean up material instance
        if (tattooMaterial != null)
        {
            Destroy(tattooMaterial);
        }
    }

    private void HandleChargeChanged(float normaizedCharge)
    {
        // Drain from top to bottom during cooldown
        if (playerOvercharge.IsInCooldown)
        {
            // Cooldown progress from 1 to 0
            targetFillAmount = playerOvercharge.CooldownProgress;
        }
        else
        {
            // Charge fill progress from 0 to 1
            targetFillAmount = normaizedCharge;
        }
    }

    private void Update()
    {
        if (tattooMaterial == null) return;

        // Smooth lerp the fill amount
        currentFillAmount = Mathf.Lerp(
            currentFillAmount,
            targetFillAmount,
            Time.deltaTime * lerpSpeed
            );

        // Update shader property
        tattooMaterial.SetFloat(fillPropertyID, currentFillAmount);
    }

}
