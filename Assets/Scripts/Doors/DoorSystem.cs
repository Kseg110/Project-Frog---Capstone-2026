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

        // The open trigger name was removed; animator-based doors will use a boolean parameter named 'Open' if present.

        [Tooltip("Animator trigger name to use to close the door when the player passes through.")]
        public string closeTriggerName = "Close";

        [Tooltip("If true the door can be closed after it was opened. If false and destroyDoor=true the door will be destroyed and cannot be closed.")]
        public bool closable = true;

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
        // When the player touches the trigger, we close the door behind them (if it was opened and closable).
        if (index < 0 || index >= links.Length) return;
        var link = links[index];
        if (link == null) return;

        if (!link.opened)
        {
            // nothing to close
            return;
        }

        if (!link.closable)
        {
            Debug.Log($"DoorSystem: Door '{link.door?.name}' is not closable (destroyed or permanent open).");
            return;
        }

        CloseDoor(link);
    }

    private void Update()
    {
        // Automatically open doors when the configured wave is reached.
        int currentWave = GetCurrentWaveSafe();
        if (currentWave == 0) return;

        for (int i = 0; i < links.Length; i++)
        {
            var link = links[i];
            if (link == null) continue;
            if (link.opened) continue;

            if (currentWave < link.minWave) continue;
            if (link.maxWave > 0 && currentWave > link.maxWave) continue;

            OpenDoor(link);
        }
    }

    // Robust helper that reads the current wave value from the WaveRoundSystem.
    // Prefer the strongly-typed, public property added to WaveRoundSystem.
    // This is simple, fast and compile-time checked.
    private int GetCurrentWaveSafe()
    {
        if (waveRoundSystem == null) return 0;

        try
        {
            return waveRoundSystem.CurrentWaveNumber;
        }
        catch
        {
            // If for some reason the property isn't present at runtime (old build, different component),
            // fall back to 0 to avoid exceptions.
            return 0;
        }
    }

    private void OpenDoor(DoorLink link)
    {
        if (link.doorAnimator != null)
        {
            // Use a boolean parameter 'Open' when available for simplicity.
            var openProp = link.doorAnimator.GetType().GetProperty("parameters");
            // Try to set a parameter named 'Open' if it exists, otherwise fall back to trigger-less open.
            try
            {
                link.doorAnimator.SetBool("Open", true);
            }
            catch
            {
                // Animator may not have the parameter; ignore and proceed.
            }
        }
        else
        {
            if (link.destroyDoor && !link.closable)
            {
                // If explicitly configured to destroy and not closable, destroy.
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
        }

        link.opened = true;
        Debug.Log($"DoorSystem: Door '{link.door?.name}' opened for wave.");
    }

    private void CloseDoor(DoorLink link)
    {
        if (link.doorAnimator != null)
        {
            if (!string.IsNullOrEmpty(link.closeTriggerName))
                link.doorAnimator.SetTrigger(link.closeTriggerName);
        }
        else
        {
            if (link.destroyDoor && !link.closable)
            {
                // was destroyed and cannot be restored
                Debug.LogWarning($"DoorSystem: Door '{link.door?.name}' was destroyed and cannot be closed/restored.");
                return;
            }

            if (link.door != null)
            {
                var colliders = link.door.GetComponentsInChildren<Collider>();
                foreach (var c in colliders)
                    c.enabled = true;

                var renderer = link.door.GetComponentInChildren<Renderer>();
                if (renderer != null)
                    renderer.enabled = true;
            }
        }

        link.opened = false;
        Debug.Log($"DoorSystem: Door '{link.door?.name}' closed after player passed.");
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
