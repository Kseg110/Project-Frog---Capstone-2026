using UnityEngine;

/// <summary>
/// Trigger placed in the scene that starts the first wave
/// when the player enters its collider.
/// 
/// This script must be placed on a GameObject with:
/// - A Collider set to "Is Trigger"
/// - A reference to WaveRoundSystem
/// 
/// When triggered:
/// - Calls WaveRoundSystem.StartFirstWave()
/// - Destroys itself so it cannot trigger twice
/// </summary>

public class WaveStartTrigger : MonoBehaviour
{
    [SerializeField] private WaveRoundSystem waveSystem;

    private bool triggered = false;

    /// <summary>
    /// Unity event fired when another collider enters this trigger.
    /// Only the Player can activate the wave start.
    /// </summary>

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        // Start the first wave through the WaveRoundSystem
        waveSystem.StartFirstWave();

        // Destroy this trigger so it cannot be activated again
        Destroy(gameObject);
    }
}