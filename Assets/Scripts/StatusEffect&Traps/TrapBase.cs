using UnityEngine;

/// <summary>
/// Base class for all traps, providing shared poison cloud spawning behavior.
/// </summary>
public abstract class TrapBase : MonoBehaviour
{
    [Header("Trap Settings")]
    [SerializeField]
    private bool isPoisonous;

    [SerializeField]
    private GameObject poisonCloudPrefab;

    /// <summary>
    /// Gets a value indicating whether this trap is poisonous.
    /// </summary>
    public bool IsPoisonous
    {
        get
        {
            return isPoisonous;
        }
    }

    protected virtual void Awake()
    {
        if (isPoisonous && poisonCloudPrefab == null)
        {
            Debug.LogError($"Class {nameof(TrapBase)} requires a poisonCloudPrefab reference when isPoisonous is true.");
        }
    }

    /// <summary>
    /// Spawns a poison cloud at the trap's position if the trap is poisonous.
    /// </summary>
    protected void SpawnPoisonCloud()
    {
        if (!isPoisonous)
        {
            return;
        }

        if (poisonCloudPrefab == null)
        {
            Debug.LogError($"Class {nameof(TrapBase)} cannot spawn a poison cloud because poisonCloudPrefab is null.");
            return;
        }

        Instantiate(poisonCloudPrefab, transform.position, Quaternion.identity);
    }
}