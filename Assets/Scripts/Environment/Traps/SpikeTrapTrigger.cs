using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpikeTrapTrigger: Place on a GameObject with a trigger Collider. When a valid target
/// enters, all linked SpikeTrapMovement components are activated; when all valid targets
/// leave, they are deactivated. This script controls MOVEMENT only — damage is handled by
/// SpikeTrap on contact with the spikes themselves.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SpikeTrapTrigger : MonoBehaviour
{
    public enum TargetMode { Player, Enemy, Both }

    [Header("Targets")]
    [Tooltip("Choose which targets activate the linked spike traps.")]
    [SerializeField] private TargetMode targetMode = TargetMode.Player;

    [Header("Linked Traps")]
    [Tooltip("Drag spike trap GameObjects here. Their SpikeTrapMovement components will be toggled.")]
    [SerializeField] private SpikeTrapMovement[] linkedTraps;

    [Header("Behaviour")]
    [Tooltip("If true, linked traps start disabled and only move when a target is inside the trigger.")]
    [SerializeField] private bool disableTrapsOnStart = true;

    [Tooltip("If true, traps stay active permanently after first activation (one-shot trigger).")]
    [SerializeField] private bool stayActiveOnce = false;

    private readonly HashSet<GameObject> occupants = new HashSet<GameObject>();
    private bool permanentlyActivated;

    private void Start()
    {
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[{nameof(SpikeTrapTrigger)}] Collider on {name} is not a trigger. Setting it now.");
            col.isTrigger = true;
        }

        if (disableTrapsOnStart)
            SetTrapsActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidTarget(other)) return;

        var root = other.transform.root.gameObject;
        bool wasEmpty = occupants.Count == 0;
        occupants.Add(root);

        if (wasEmpty && !permanentlyActivated)
        {
            SetTrapsActive(true);
            if (stayActiveOnce)
                permanentlyActivated = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (permanentlyActivated) return;

        occupants.Remove(other.transform.root.gameObject);

        if (occupants.Count == 0)
            SetTrapsActive(false);
    }

    private bool IsValidTarget(Collider col)
    {
        if ((targetMode == TargetMode.Player || targetMode == TargetMode.Both)
            && col.gameObject.CompareTag("Player"))
            return true;

        if (targetMode == TargetMode.Enemy || targetMode == TargetMode.Both)
        {
            if (col.gameObject.CompareTag("Enemy")) return true;
            if (col.GetComponentInParent<EnemyBase>() != null) return true;
        }

        return false;
    }

    private void SetTrapsActive(bool active)
    {
        if (linkedTraps == null) return;

        foreach (var trap in linkedTraps)
            if (trap != null)
                trap.enabled = active;
    }

    private void OnDisable() => occupants.Clear();

    private void OnDrawGizmosSelected()
    {
        if (linkedTraps == null) return;

        Gizmos.color = Color.red;
        foreach (var trap in linkedTraps)
            if (trap != null)
                Gizmos.DrawLine(transform.position, trap.transform.position);
    }
}