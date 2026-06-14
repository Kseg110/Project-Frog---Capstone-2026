using UnityEngine;

public class TestHealthListen : MonoBehaviour
{
    [SerializeField] private Health playerHealthRef;

    // Variable to cache the health value for OnGUI to read
    private float _cachedHealth;

    private void Awake()
    {
        playerHealthRef.OnHealthChanged += PlayerHealthRef_OnHealthChanged;
    }

    private void Start()
    {
        if (playerHealthRef != null)
        {
            _cachedHealth = playerHealthRef.CurrentHealth;
        }
    }

    private void OnDestroy()
    {
        playerHealthRef.OnHealthChanged -= PlayerHealthRef_OnHealthChanged;
    }

    private void PlayerHealthRef_OnHealthChanged(float newHealthValue)
    {
        // Store the value whenever it changes
        _cachedHealth = newHealthValue;
    }

    private void OnGUI()
    {
        // Position and size of the UI label: (X, Y, Width, Height)
        Rect labelRect = new Rect(20, 20, 200, 40);

        // Styling the text so it is easy to read
        GUI.skin.label.fontSize = 20;

        // Draw the text on the screen
        GUI.Label(labelRect, $"Health: {_cachedHealth}");
    }
}
