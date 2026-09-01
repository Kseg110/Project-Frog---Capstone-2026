using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DoorSystemUpdated
/// - Handles opening/closing of area doors based on wave progression.
/// - Plays a wiggle animation clip on individual doors ONCE when opening and ONCE when closing.
/// </summary>
public class DoorSystemUpdated : MonoBehaviour
{
    [Serializable]
    public class DoorLink
    {
        [Tooltip("The door GameObject to lower when active.")]
        public GameObject door;

        [Tooltip("Child trigger collider used to close the door behind the player.")]
        public GameObject closeTrigger;

        [Tooltip("Minimum wave number when this door opens.")]
        public int minWave = 1;

        [Tooltip("Optional maximum wave. Set to 0 for no maximum.")]
        public int maxWave = 0;

        [Tooltip("Distance to lower the door on Y axis.")]
        public float lowerDistance = 5f;

        [Tooltip("Speed the door lowers.")]
        public float lowerSpeed = 2f;

        [Tooltip("Speed the door rises back up.")]
        public float riseSpeed = 1f;

        [Tooltip("Door will not open until set ready via wave system.")]
        public bool ready = false;

        [Tooltip("Cooldown after player closes door before it can reopen.")]
        public float reopenCooldown = 1.0f;

        [Tooltip("Optional Animator override attached directly to this specific door.")]
        public Animator doorAnimator;

        [NonSerialized] public Vector3 originalPosition;
        [NonSerialized] public bool opened = false;
        [NonSerialized] public bool playerClosed = false;
        [NonSerialized] public float lastClosedTime = -Mathf.Infinity;
        [NonSerialized] public Coroutine moveRoutine;
    }

    public void SetDoorReady(int index)
    {
        if (index < 0 || index >= links.Length) return;
        links[index].ready = true;
    }

    [Header("Door Links")]
    [SerializeField] private DoorLink[] links = Array.Empty<DoorLink>();

    [Header("References")]
    [SerializeField] private WaveRoundSystem waveRoundSystem;
    [SerializeField] private int currentWave;

    [Header("Animation Settings")]
    [Tooltip("Name of the state inside the door's Animator Controller.")]
    [SerializeField] private string wiggleStateName = "DoorWiggle";

    [Tooltip("Name of the Trigger parameter if using Animator parameters instead of state names.")]
    [SerializeField] private string wiggleTriggerName = "Wiggle";

    [Tooltip("Set true if using an Animator Trigger parameter instead of direct state Play.")]
    [SerializeField] private bool useTriggerParameter = false;

    [Tooltip("Delay in seconds to let the wiggle animation play before sliding movement begins.")]
    [SerializeField] private float wiggleDuration = 0.35f;

    private void Awake()
    {
        if (waveRoundSystem == null)
            waveRoundSystem = FindAnyObjectByType<WaveRoundSystem>();

        for (int i = 0; i < links.Length; i++)
        {
            var link = links[i];
            if (link == null) continue;

            // ONLY attach trigger relays to the designated closeTrigger object
            if (link.closeTrigger != null)
                AttachRelaysToTriggers(link.closeTrigger, i);

            if (link.door != null)
            {
                link.originalPosition = link.door.transform.position;

                if (link.doorAnimator == null)
                    link.doorAnimator = link.door.GetComponent<Animator>();
            }
        }
    }

    private void Update()
    {
        if (waveRoundSystem != null)
            currentWave = waveRoundSystem.CurrentWave;

        for (int i = 0; i < links.Length; i++)
        {
            var link = links[i];
            if (link == null || link.opened || link.playerClosed) continue;

            if (Time.time - link.lastClosedTime < link.reopenCooldown) continue;
            if (currentWave < link.minWave) continue;
            if (link.maxWave > 0 && currentWave > link.maxWave) continue;
            if (!link.ready) continue;

            OpenDoor(link);
        }
    }

    private void AttachRelaysToTriggers(GameObject root, int index)
    {
        var cols = root.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            if (c == null || !c.isTrigger) continue;

            var go = c.gameObject;
            var relay = go.GetComponent<DoorTriggerRelay>();
            if (relay == null) relay = go.AddComponent<DoorTriggerRelay>();
            relay.owner = this;
            relay.index = index;
        }
    }

    /// <summary>
    /// Triggers wiggle animation on demand (e.g., when attempting to open a locked door).
    /// </summary>
    public void TriggerDoorWiggle(int index)
    {
        if (index < 0 || index >= links.Length) return;
        PlayWiggleAnimation(links[index]);
    }

    private void PlayWiggleAnimation(DoorLink link)
    {
        Animator anim = link.doorAnimator;
        if (anim == null && link.door != null)
            anim = link.door.GetComponent<Animator>();

        if (anim == null) return;

        if (useTriggerParameter && !string.IsNullOrEmpty(wiggleTriggerName))
        {
            anim.SetTrigger(wiggleTriggerName);
        }
        else if (!string.IsNullOrEmpty(wiggleStateName))
        {
            anim.Play(wiggleStateName, 0, 0f);
        }
    }

    private void MoveDoor(DoorLink link, Vector3 target, float speed)
    {
        if (link.moveRoutine != null)
            StopCoroutine(link.moveRoutine);

        link.moveRoutine = StartCoroutine(MoveDoorRoutine(link, target, speed));
    }

    private IEnumerator MoveDoorRoutine(DoorLink link, Vector3 target, float speed)
    {
        if (link.door == null) yield break;

        PlayWiggleAnimation(link);

        if (wiggleDuration > 0f)
        {
            yield return new WaitForSeconds(wiggleDuration);
        }

        while (Vector3.Distance(link.door.transform.position, target) > 0.01f)
        {
            link.door.transform.position = Vector3.MoveTowards(
                link.door.transform.position,
                target,
                speed * Time.deltaTime
            );

            yield return null;
        }

        link.door.transform.position = target;
        link.moveRoutine = null;
    }

    private void OpenDoor(DoorLink link)
    {
        if (link.door == null)
        {
            link.opened = true;
            return;
        }

        Vector3 target = link.originalPosition + Vector3.down * link.lowerDistance;
        MoveDoor(link, target, link.lowerSpeed);
        link.opened = true;
    }

    private void CloseDoor(DoorLink link)
    {
        // Ignore close requests if the door hasn't opened yet
        if (!link.opened) return;

        link.ready = false;

        if (link.moveRoutine != null)
        {
            StopCoroutine(link.moveRoutine);
            link.moveRoutine = null;
        }

        MoveDoor(link, link.originalPosition, link.riseSpeed);

        link.opened = false;
        link.lastClosedTime = Time.time;
        link.playerClosed = true;

        if (waveRoundSystem != null)
            waveRoundSystem.OnPlayerReachedNextArea();
    }

    internal void OnTriggerActivated(int index)
    {
        if (index < 0 || index >= links.Length) return;
        CloseDoor(links[index]);
    }

    public void ResetPlayerClosed(int linkIndex)
    {
        if (linkIndex < 0 || linkIndex >= links.Length) return;
        links[linkIndex].playerClosed = false;
    }

    private class DoorTriggerRelay : MonoBehaviour
    {
        public DoorSystemUpdated owner;
        public int index;

        private void OnTriggerEnter(Collider other)
        {
            if (owner == null) return;
            if (other.CompareTag("Player")) owner.OnTriggerActivated(index);
        }
    }
}
