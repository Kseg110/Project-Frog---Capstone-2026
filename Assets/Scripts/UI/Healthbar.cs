using UnityEngine;
using UnityEngine.UI;

public class Healthbar: MonoBehaviour
{
    [SerializeField] private Image foregroundImage;
    [SerializeField] private Transform uiContainer;

    private Camera mainCamera;
    [SerializeField] private Health health;
    public void UpdateHealthBar(float maxHealth, float curHealth)
    {
        // Change the fill amount of the foreground to the percentage of health left
        foregroundImage.fillAmount = curHealth / maxHealth;
    }

    private void Start()
    {
        if (health == null)
        {
            Debug.LogError($"Object {this} has no health component!");
        }
        mainCamera = Camera.main;
        health.OnHealthChanged += Health_OnHealthChanged;
    }

    private void Health_OnHealthChanged(float newHealth)
    {
        UpdateHealthBar(health.maxHealth, newHealth);
    }

    private void LateUpdate()
    {
        // Point the healthbar to the camera
        // Only rotate the UI container, not the main GameObject
        if (uiContainer != null && mainCamera != null)
        {
            uiContainer.forward = mainCamera.transform.forward;
        }
    }
}