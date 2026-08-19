using UnityEngine;
using FMODUnity;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerTongueAttack))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerTongueHealing : MonoBehaviour
{
    [SerializeField] private float healAmountPerFly;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Healing Slow")]
    [Tooltip("Movement speed multiplier applied while using a Healing Fly. 0.5 = half speed.")]
    [SerializeField] private float healSlowMultiplier = 0.5f;

    [Tooltip("How long the slow lasts after using a Healing Fly, in seconds.")]
    [SerializeField] private float healSlowDuration = 1f;

    private PlayerTongueAttack playerTongueAttack;
    private PlayerMovement playerMovement;
    private Health playerHealth;

    [Header("FMod Events")]
    [SerializeField] private EventReference flyGatherEvent;

    private int numberOfFliesAttached = 0; // Fly counter for how many times the player heals when retracting

    // Heal-slow state, mirrors the basic-shot slow in PlayerAttacks.
    private bool isHealSlowed = false;
    private float healSlowTimer = 0f;

    private void Awake()
    {
        playerTongueAttack = GetComponent<PlayerTongueAttack>();
        playerMovement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<Health>();

        // Subscribe to the playerTongueAttack's finish event so we can heal after retraction
        playerTongueAttack.OnTongueFinished += GainFly;
    }

    private void OnDestroy()
    {
        // Unsubscribe from event to prevent memory leaks
        playerTongueAttack.OnTongueFinished -= GainFly;
    }

    private void Update()
    {
        // Tick down the heal slow, then release the modifier when it expires.
        if (isHealSlowed)
        {
            healSlowTimer -= Time.deltaTime;
            if (healSlowTimer <= 0f)
            {
                isHealSlowed = false;
                playerMovement.RemoveSpeedModifier(this);
            }
        }
    }

    // Called from InventoryManager.ConsumeFly the moment a Healing Fly is actually used, so the slow only fires on a successful consume. 
    public void ApplyHealSlow()
    {
        if (!isHealSlowed)
        {
            playerMovement.AddSpeedModifier(this, healSlowMultiplier);
            isHealSlowed = true;
        }

        healSlowTimer = Mathf.Max(healSlowTimer, healSlowDuration);
    }

    private void OnDisable()
    {
        // Insurance: don't leave a lingering modifier in PlayerMovement if this component is disabled mid-slow.
        if (isHealSlowed)
        {
            playerMovement.RemoveSpeedModifier(this);
            isHealSlowed = false;
            healSlowTimer = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playerTongueAttack.IsActive && other.CompareTag("Fly"))
        {
            AttachFly();
            Destroy(other.gameObject);
            RuntimeManager.PlayOneShot(flyGatherEvent, transform.position);
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

    public void DebugGainFly()
    {
        GainFly();
    }
}