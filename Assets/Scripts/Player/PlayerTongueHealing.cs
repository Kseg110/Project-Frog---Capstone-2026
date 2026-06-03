using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerTongueAttack))]
public class PlayerTongueHealing : MonoBehaviour
{
    [SerializeField] private float healAmountPerFly;

    [SerializeField] private InventoryManager inventoryManager;
    private PlayerTongueAttack playerTongueAttack;
    private Health playerHealth;

    private int numberOfFliesAttached = 0; // Fly counter for how many times the player heals when retracting

    private void Awake()
    {
        playerTongueAttack = GetComponent<PlayerTongueAttack>();
        playerHealth = GetComponent<Health>();

        // Subscribe to the playerTongueAttack's finish event so we can heal after retraction
        playerTongueAttack.OnTongueFinished += GainFly;
    }

    private void OnDestroy()
    {
        // Unsubscribe from event to prevent memory leaks
        playerTongueAttack.OnTongueFinished -= GainFly;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playerTongueAttack.IsActive && other.CompareTag("Fly"))
        {
            AttachFly();
            Destroy(other.gameObject);
        }
    }

    private void AttachFly()
    {
        numberOfFliesAttached++;
    }

    private void GainFly()
    {
        if (playerHealth.IsMaxHP())
        {
            inventoryManager.GainFlyInInventory(numberOfFliesAttached);
        }
        else
        {
            HealPlayer(numberOfFliesAttached);
        }

        numberOfFliesAttached = 0;
    }

    public void HealPlayer(int numberOfFlies)
    {
        playerHealth.Heal(healAmountPerFly * numberOfFlies);
    }
}