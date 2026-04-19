using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to a trap parent object. The script finds a child GameObject tagged "trap"
/// (that should contain an isTrigger Collider) and forwards trigger events to this component.
/// When a GameObject tagged "Player" enters the child trigger the trap will start shooting
/// dart prefabs every `fireInterval` seconds; it stops when all players leave the trigger.
/// </summary>
public class DartTrap : MonoBehaviour
{
    [Header("Dart")]
    [SerializeField] private GameObject dartPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float dartSpeed = 20f;
    [SerializeField] private float dartLifetime = 10f;

    [Header("Timing")]
    [Tooltip("Seconds between shots while trigger is active.")]
    [SerializeField] private float fireInterval = 5f;

    private readonly HashSet<GameObject> playersInTrigger = new();
    private Coroutine shootingRoutine;
    private GameObject trapTriggerChild;

    private void Start()
    {
        // Find first child tagged "trap"
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject != gameObject && t.gameObject.CompareTag("trap"))
            {
                trapTriggerChild = t.gameObject;
                break;
            }
        }

        if (trapTriggerChild == null)
        {
            Debug.LogWarning($"[{nameof(DartTrap)}] No child with tag \"trap\" found under {name}.");
            return;
        }

        // Ensure the child has a Collider set as trigger
        var col = trapTriggerChild.GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"[{nameof(DartTrap)}] Child tagged \"trap\" on {trapTriggerChild.name} has no Collider.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"[{nameof(DartTrap)}] Collider on {trapTriggerChild.name} is not marked as isTrigger. Mark it as trigger for trap activation.");
        }

        // Add or get forwarding component so the child's trigger events are forwarded here
        var forwarder = trapTriggerChild.GetComponent<TrapTriggerForwarder>() ?? trapTriggerChild.AddComponent<TrapTriggerForwarder>();
        forwarder.parent = this;
    }

    // Called by the trigger forwarder when something enters the child trigger
    internal void OnChildTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        playersInTrigger.Add(other.gameObject);

        if (playersInTrigger.Count == 1)
        {
            StartShooting();
        }
    }

    // Called by the trigger forwarder when something exits the child trigger
    internal void OnChildTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        playersInTrigger.Remove(other.gameObject);

        if (playersInTrigger.Count == 0)
        {
            StopShooting();
        }
    }

    private void StartShooting()
    {
        if (shootingRoutine != null) return;
        shootingRoutine = StartCoroutine(ShootingLoop());
    }

    private void StopShooting()
    {
        if (shootingRoutine != null)
        {
            StopCoroutine(shootingRoutine);
            shootingRoutine = null;
        }
    }

    private IEnumerator ShootingLoop()
    {
        // Fire immediately on activation, then wait between shots
        while (true)
        {
            ShootOnce();
            yield return new WaitForSeconds(fireInterval);
        }
    }

    private void ShootOnce()
    {
        if (dartPrefab == null)
        {
            Debug.LogWarning($"[{nameof(DartTrap)}] No dartPrefab assigned on {name}.");
            return;
        }

        if (shootPoint == null)
        {
            Debug.LogWarning($"[{nameof(DartTrap)}] No shootPoint assigned on {name}.");
            return;
        }

        var dart = Instantiate(dartPrefab, shootPoint.position, shootPoint.rotation);
        var rb = dart.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = shootPoint.right * dartSpeed;
        }

        Destroy(dart, dartLifetime);
    }

    private void OnDisable()
    {
        StopShooting();
    }
}

/// <summary>
/// Lightweight forwarder put on the child trigger object that calls back into the parent TrapShoot.
/// This class is intentionally in the same file to keep the trap implementation together.
/// </summary>
public class TrapTriggerForwarder : MonoBehaviour
{
    [HideInInspector] public DartTrap parent;

    private void OnTriggerEnter(Collider other)
    {
        parent?.OnChildTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        parent?.OnChildTriggerExit(other);
    }
}
