using UnityEngine;
using UnityEngine.UI;

// Simple System hooking into Health.cs, PlayerMovement.cs and soon the Overcharge system that will display the Player Health, and Dash Cooldown in radial bar formats. Overcharge is a traditional bar. -E.M
// Update: Implemented color based states depending on the Player's amount of health. 100 - 51% -> Green, <= 50% -> Yellow, <= 25% -> Red. Also allowed functionality for Gradients. -E.M
public class UIPlayerHUD : MonoBehaviour
{
    [Header("Health Wheel")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private float healthLerpSpeed = 5f;

    [Header("Health Colours")]
    [SerializeField] private Gradient healthGradient;
    [SerializeField] private Color HealthyColor = Color.green;
    [SerializeField] private Color WarningColor = Color.yellow;
    [SerializeField] private Color CriticalColor = Color.red;

    [Header("Dash Wheel")]
    [SerializeField] private Image DashFillImage;

    private float targetHealthFill = 1f;

    //[Header("Overcharge Bar")]
    //[SerializeField] private Image OverchargeFillImage;

    // Calls from the health system whenever HP changes.
    public void UpdateHealth(float normalized)
    {
        targetHealthFill = Mathf.Clamp01(normalized);
        healthFillImage.color = healthGradient.Evaluate(normalized);

        //if (normalized > 0.5f)
        //    healthFillImage.color = HealthyColor;

        //else if (normalized > 0.25f)
        //    healthFillImage.color = WarningColor;

        //else healthFillImage.color = CriticalColor;

    }

    private void Update()
    {
        // Smoothly lerp the fill toward the target value
        healthFillImage.fillAmount = Mathf.Lerp(
            healthFillImage.fillAmount,
            targetHealthFill,
            Time.deltaTime * healthLerpSpeed
        );
    }

    // Calls from the dash system every frame (or whenever the cooldown ticks).
    public void UpdateDashCooldown(float normalized)
    {
        DashFillImage.fillAmount = Mathf.Clamp01(normalized);
    }

    // Calls from the overcharge system (once merged into main).
    //public void UpdateOverchargeBar(float normalized)
    //{
    //    OverchargeFillImage.fillAmount = Mathf.Clamp01(normalized);
    //}
}