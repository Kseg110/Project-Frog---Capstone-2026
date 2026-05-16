using System;
using UnityEngine;

/// <summary>
/// DoorSystem
/// - Configure pairs of door GameObjects and trigger GameObjects
/// - Each pair has a minimum wave (and optional maximum wave) when it becomes active
/// - When the player enters the configured trigger and the wave requirement is met,
///   the door will open (via Animator trigger), or be destroyed/disabled.
///
/// Usage:
/// - Create DoorSystem on a manager GameObject and add entries in the inspector.
/// - Set the WaveRoundSystem reference (optional; will be found automatically if null).
/// - Triggers must have a Collider set to isTrigger. The script will attach a small relay
///   component to each trigger at runtime to listen for player entry.
/// </summary>
public class DoorSystem : MonoBehaviour
{
    [Serializable]
    public class DoorLink
    {
        [Tooltip("The door GameObject that will be opened or destroyed.")]
        public GameObject door;

        [Tooltip("The trigger GameObject the player must enter to open the door. Must have a Collider with isTrigger=true.")]
        public GameObject trigger;

        [Tooltip("Minimum wave number (1-based) required for this trigger to work.")]
        public int minWave = 1;

        [Tooltip("Optional maximum wave (1-based). Set to 0 for no maximum.")]
        public int maxWave = 0;

        [Tooltip("If true the door GameObject will be destroyed when opened. Otherwise its Collider will be disabled.")]
        public bool destroyDoor = true;

        [Tooltip("Optional Animator on the door. If provided, the animator's trigger named OpenTriggerName will be invoked instead of immediately destroying/disabling the door.")]
        public Animator doorAnimator;

        [Tooltip("Animator trigger name to use to open the door.")]
        public string openTriggerName = "Open";

        [NonSerialized]
        public bool opened = false;
    }

    [Header("Door Links")]
    [SerializeField]
    private DoorLink[] links = Array.Empty<DoorLink>();

    [Header("References")]
    [SerializeField]
    private WaveRoundSystem waveRoundSystem;

    private void Awake()
    {
        if (waveRoundSystem == null)
            waveRoundSystem = FindObjectOfType<WaveRoundSystem>();

        // Attach relays to triggers so we get OnTriggerEnter callbacks
        for (int i = 0; i < links.Length; i++)
        {
            var link = links[i];
            if (link == null || link.trigger == null)
            {
                Debug.LogWarning($"DoorSystem: Link {i} has no trigger assigned.");
                continue;
            }

            Collider triggerCollider = link.trigger.GetComponent<Collider>();
            if (triggerCollider == null)
            {
                Debug.LogWarning($"DoorSystem: Trigger on link {i} has no Collider component.");
                continue;
            }

            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning($"DoorSystem: Trigger Collider on '{link.trigger.name}' should have isTrigger = true.");
            }

            // Add or configure relay
            var relay = link.trigger.GetComponent<DoorTriggerRelay>();
            if (relay == null)
                relay = link.trigger.AddComponent<DoorTriggerRelay>();

            relay.owner = this;
            relay.index = i;
        }
    }

    /// <summary>
    /// Called by the trigger relay when the player enters a trigger.
    /// </summary>
    internal void OnTriggerActivated(int index)
    {
        if (index < 0 || index >= links.Length) return;
        var link = links[index];
        if (link == null) return;
        if (link.opened) return;

        int currentWave = GetCurrentWaveSafe();

        if (currentWave == 0)
        {
            // Wave system not started or no waves yet
            Debug.Log($"DoorSystem: Wave not started yet. Door '{link.door?.name}' requires wave {link.minWave}.");
            return;
        }

        if (currentWave < link.minWave)
        {
            Debug.Log($"DoorSystem: Door '{link.door?.name}' requires wave {link.minWave} (current: {currentWave}).");
            return;
        }

        if (link.maxWave > 0 && currentWave > link.maxWave)
        {
            Debug.Log($"DoorSystem: Door '{link.door?.name}' is only available up to wave {link.maxWave} (current: {currentWave}).");
            return;
        }

        OpenDoor(link);
    }

    // Robust helper that reads the current wave value from the WaveRoundSystem.
    // Supports multiple possible property/method/field names so DoorSystem remains
    // compatible if the WaveRoundSystem API was renamed/changed.
    private int GetCurrentWaveSafe()
    {
        if (waveRoundSystem == null) return 0;
        var type = waveRoundSystem.GetType();
        string[] names = { "CurrentWaveIndex", "CurrentWave", "CurrentRound", "CurrentRoundIndex", "CurrentWaveNumber", "CurrentWaveId" };

        foreach (var name in names)
        {
            var prop = type.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop != null)
            {
                var val = prop.GetValue(waveRoundSystem);
                if (val is int i) return i;
                if (val is long l) return (int)l;
            }
        }

        foreach (var name in names)
        {
            var method = type.GetMethod(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (method != null)
            {
                var val = method.Invoke(waveRoundSystem, null);
                if (val is int i) return i;
                if (val is long l) return (int)l;
            }
        }

        foreach (var name in names)
        {
            var field = type.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var val = field.GetValue(waveRoundSystem);
                if (val is int i) return i;
                if (val is long l) return (int)l;
            }
        }

        return 0;
    }

    private void OpenDoor(DoorLink link)
    {
        if (link.doorAnimator != null)
        {
            link.doorAnimator.SetTrigger(link.openTriggerName);
        }
        else if (link.destroyDoor)
        {
            if (link.door != null)
                Destroy(link.door);
        }
        else
        {
            if (link.door != null)
            {
                // Try to disable colliders and optionally make the object invisible / non-blocking
                var colliders = link.door.GetComponentsInChildren<Collider>();
                foreach (var c in colliders)
                    c.enabled = false;

                var renderer = link.door.GetComponentInChildren<Renderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        link.opened = true;
        Debug.Log($"DoorSystem: Door '{link.door?.name}' opened for wave.");
    }

    /// <summary>
    /// Small helper MonoBehaviour that relays trigger events back to the DoorSystem.
    /// This is added to the trigger GameObjects at runtime.
    /// </summary>
    private class DoorTriggerRelay : MonoBehaviour
    {
        public DoorSystem owner;
        public int index;

        private void OnTriggerEnter(Collider other)
        {
            if (owner == null) return;
            if (other.CompareTag("Player"))
            {
                owner.OnTriggerActivated(index);
            }
        }
    }
}
