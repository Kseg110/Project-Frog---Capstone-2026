using UnityEngine;

public class WaveObjectTrigger : MonoBehaviour
{
    [Header("Wave System")]
    public WaveRoundSystem waveSystem;

    [Header("Trigger")]
    [Tooltip("Trigger when this wave is reached")]
    public int targetWave = 3;

    [Header("Objects")]
    public GameObject[] targetObjects;

    [Header("Action")]
    public bool disableObjects = true;

    [Header("Debug")]
    public bool triggered;

    void Update()
    {
        // =====================================================
        // SAFETY
        // =====================================================
        if (waveSystem == null)
            return;

        if (triggered)
            return;

        // =====================================================
        // CHECK CURRENT WAVE
        // =====================================================
        int currentWave =
            waveSystem.CurrentWave + 1;

        // =====================================================
        // TRIGGER
        // =====================================================
        if (currentWave >= targetWave)
        {
            triggered = true;

            ApplyAction();
        }
    }

    // =====================================================
    // APPLY ACTION
    // =====================================================
    void ApplyAction()
    {
        if (targetObjects == null ||
            targetObjects.Length == 0)
            return;

        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] == null)
                continue;

            targetObjects[i].SetActive(
                !disableObjects
            );
        }

        Debug.Log(
            "WaveObjectTrigger Triggered At Wave: " +
            targetWave
        );
    }
}