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

    [Header("Dash Box")]
    [SerializeField] private Image DashFillImage;

    [Header("Overcharge Wheel")]
    [SerializeField] private Image OverchargeFillImage;
    [SerializeField] private float OverChargeLerpSpeed = 3f;

    private float targetHealthFill = 1f;
    private float targetOverchargeFill = 0f;

    [SerializeField] private Health playerHealth;

    private void Awake()
    {
        UpdateHealth(1f);
        playerHealth.OnHealthChanged += PlayerHealth_OnHealthChanged;
    }

    private void PlayerHealth_OnHealthChanged(float newHealth)
    {
        Debug.Log(newHealth);
        float normalized = newHealth / playerHealth.maxHealth;
        UpdateHealth(normalized);
    }

    //[Header("Overcharge Bar")]
    //[SerializeField] private Image OverchargeFillImage;

    // Calls from the health system whenever HP changes.
    public void UpdateHealth(float normalizedHealth)
    {
        //float normalized = newHealth / playerHealth.maxHealth;
        targetHealthFill = Mathf.Clamp01(normalizedHealth);
        healthFillImage.color = healthGradient.Evaluate(normalizedHealth);

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

        // Smoothly Lerp overchage fill
        if (OverchargeFillImage != null)
        {
            OverchargeFillImage.fillAmount = Mathf.Lerp(
                OverchargeFillImage.fillAmount,
                targetOverchargeFill,
                Time.deltaTime * OverChargeLerpSpeed
                );
        }
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

    public void UpdateOverchargeWheel(float normalized)
    {
        targetOverchargeFill = Mathf.Clamp01(normalized);
    }

    public void ShowHUD()
    {
        gameObject.SetActive(true);
    }

    public void HideHUD()
    {
        gameObject.SetActive(false);
    }
}