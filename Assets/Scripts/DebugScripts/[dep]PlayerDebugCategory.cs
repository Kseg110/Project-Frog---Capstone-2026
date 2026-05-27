using UnityEngine;
using UnityEngine.UI;

public class PlayerDebugCategory : MonoBehaviour
{
    public CombatStatistics stats;
    public PlayerTakeDamage takeDamage;

    public Slider playerSpeedSlider;
    public Slider tongueSpeedSlider;
    public Toggle godModeToggle;

    private void Start()
    {
        // Initialize UI
        playerSpeedSlider.value = stats.playerSpeed;
        tongueSpeedSlider.value = stats.tongueExtendSpeed;
        godModeToggle.isOn = takeDamage.isGod;

        // Apply changes
        playerSpeedSlider.onValueChanged.AddListener(v => stats.playerSpeed = v);
        tongueSpeedSlider.onValueChanged.AddListener(v => stats.tongueExtendSpeed = v);
    }
    public void ToggleGodMode(bool toggleValue)
    {
        if (toggleValue)
        {
            // Enable god mode: make player invulnerable and optionally add visual feedback
            takeDamage.isGod = true;
            // You could also add a visual indicator here, like changing the player's color or adding an effect
        }
        else
        {
            // Disable god mode: revert to normal vulnerability
            takeDamage.isGod = false;
            // Revert any visual indicators if you added them when enabling god mode
        }

        Debug.Log("God mode toggled: " + toggleValue);
    }
}

