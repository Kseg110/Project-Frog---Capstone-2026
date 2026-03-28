using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerTongueAttack))]
public class PlayerTongueHealing : MonoBehaviour
{
    [SerializeField] private float healAmountPerFly;
    
    private PlayerTongueAttack playerTongueAttack;
    private Health playerHealth;

    private int numberOfFliesAttached = 0; // Fly counter for how many times the player heals when retracting

    private void Awake()
    {
        playerTongueAttack = GetComponent<PlayerTongueAttack>();
        playerHealth = GetComponent<Health>();

        // Subscribe to the playerTongueAttack's finish event so we can heal after retraction
        playerTongueAttack.OnTongueFinished += HealPlayer;
    }

    private void OnDestroy()
    {
        // Unsubscribe from event to prevent memory leaks
        playerTongueAttack.OnTongueFinished -= HealPlayer;
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

    private void HealPlayer()
    {
        playerHealth.Heal(healAmountPerFly * numberOfFliesAttached);

        // Reset fly counter
        numberOfFliesAttached = 0;
    }
}