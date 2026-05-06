using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PoisonTrap : MonoBehaviour
{
    [Header("Poison Trap Settings")]
    [SerializeField] private GameObject poisonCloudPrefab;
    [SerializeField] private float cloudDuration = 10f;       // How long the cloud stays alive
    [SerializeField] private float trapCooldown = 5f;         // How long before trap can trigger again

    private bool isOnCooldown = false;

    private void Awake()
    {
        Collider trapCollider = GetComponent<Collider>();

        if (trapCollider == null)
        {
            Debug.LogError($"Class {nameof(PoisonTrap)} requires a Collider component.");
            return;
        }

        if (!trapCollider.isTrigger)
        {
            Debug.LogError($"Class {nameof(PoisonTrap)} requires the Collider to be set as a trigger.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryActivateTrap(other);
    }

    private void TryActivateTrap(Collider other)
    {
        if (isOnCooldown)
            return;

        if (other.GetComponent<StatusEffectHandler>() == null)
            return; // Only activate if something with status effects enters

        ActivateTrap();
    }

    private void ActivateTrap()
    {
        isOnCooldown = true;

        // Spawn the cloud
        GameObject cloud = Instantiate(poisonCloudPrefab, transform.position, Quaternion.identity);

        // Destroy cloud after duration
        Destroy(cloud, cloudDuration);

        // Start cooldown
        Invoke(nameof(ResetCooldown), trapCooldown);
    }

    private void ResetCooldown()
    {
        isOnCooldown = false;
    }
}
