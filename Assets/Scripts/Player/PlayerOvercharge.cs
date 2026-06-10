using UnityEngine;
using System;

public class PlayerOvercharge : MonoBehaviour
{

    [Header("Overcharge Settings")]
    [SerializeField] private float chargedTime = 20f;
    [SerializeField] private float chargeCooldown = 15f;
    [SerializeField] private float chargeDecayRate = 1f; // Charge decay countdown when not tethered
    [SerializeField] private float trailDamage = 5f; // temporary for testing

    [Header("References")]
    [SerializeField] private PlayerAnchor playerAnchor;
    [SerializeField] private UIPlayerHUD playerHUD;
    [SerializeField] private PlayerOverchargeVFX overchargeVFX;
    [SerializeField] private OverchargeTrailCollider trailCollider;

    // Overcharge Events
    public event Action<float> OnChargeChanged; 
    public event Action OnOverchargeActivated;
    public event Action OnCooldownComplete;

    // Overcharge State
    private float currentChargeTime;
    private float currentCooldownTime;
    private bool isInCooldown;
    private bool isOvercharged;
    private AnchorBase lastTetheredAnchor;

    // Overcharge Properties
    public float CurrentChargeTime => currentChargeTime;
    public float ChargeProgress => Mathf.Clamp01(currentChargeTime / chargedTime);
    public float CooldownProgress => isInCooldown ? Mathf.Clamp01(currentCooldownTime / chargeCooldown) : 0f;
    public bool IsInCooldown => isInCooldown;
    public bool IsOvercharged => isOvercharged;
    public AnchorBase LastTetheredAnchor => lastTetheredAnchor;
    public bool IsTrailActive => isOvercharged || isInCooldown;

    private void Awake()
    {
        if (playerAnchor == null)
        {
            playerAnchor = GetComponent<PlayerAnchor>();
        }
        if (overchargeVFX == null)
        {
            overchargeVFX = GetComponentInChildren<PlayerOverchargeVFX>();
        }
        if (trailCollider == null)
        {
            trailCollider = GetComponentInChildren<OverchargeTrailCollider>();
        }

        currentChargeTime = 0f;
        currentCooldownTime = 0f;
        isInCooldown = false;
        isOvercharged = false;

        // subscribe to trail collision events
        if (trailCollider != null)
        {
            trailCollider.OnEnemyHit += HandleEnemyHit;
        }
    }

    private void OnDestroy()
    {
       // unsub from collision events
       if (trailCollider != null)
        {
            trailCollider.OnEnemyHit -= HandleEnemyHit;
        }
    }

    private void Update()
    {
        if (isInCooldown)
        {
            UpdateCooldown();
        }
        else
        {
            UpdateCharge();
        }
    }

    private void UpdateCharge()
    {
        bool isTethered = playerAnchor.IsTethered;

        if (isTethered)
        {
            // Stores current anchor type
            lastTetheredAnchor = playerAnchor.CurrentAnchor;
            // Charge while tethered
            currentChargeTime += Time.deltaTime;
            // Fully charged check
            if (currentChargeTime >= chargedTime)
            {
                currentChargeTime = chargedTime;
                ActivateOvercharge();
            }
        }
        else
        {
            // Decay Charge timer when not tethered
            if (currentChargeTime > 0)
            {
                currentChargeTime -= Time.deltaTime * chargeDecayRate;
                currentChargeTime = Mathf.Max(0f, currentChargeTime);
            }
        }

        // Tell listeneres of charge change
        OnChargeChanged?.Invoke(ChargeProgress);

        // Update Player HUD
        if (playerHUD != null)
        {
            playerHUD.UpdateOverchargeWheel(ChargeProgress);
        }
    }

    private void UpdateCooldown()
    {
        currentCooldownTime -= Time.deltaTime;

        if(currentCooldownTime <= 0f)
        {
            currentCooldownTime = 0f;
            isInCooldown = false;
            OnCooldownComplete?.Invoke();

            EndOverchargeTrail();
        }

        float fillAmount = CooldownProgress;

        // Update HUD
        if (playerHUD != null)
        {
            playerHUD.UpdateOverchargeWheel(fillAmount);
        }
    }

    private void ActivateOvercharge()
    {
        isOvercharged = true;

        // Untether Player
        playerAnchor.ReleaseTether();

        // Start trail effects
        StartOverchargeTrail();

        // Begin Cooldown
        StartCooldown();

        // Notify Listeners
        OnOverchargeActivated?.Invoke();

        Debug.Log($"Overcharge activated Anchor Type: {(lastTetheredAnchor != null ? lastTetheredAnchor.GetType().Name : "None")}");
    }

    private void StartCooldown()
    {
        isInCooldown = true;
        currentCooldownTime = chargeCooldown;
        currentChargeTime = 0f;
        isOvercharged = false;
    }

    private void StartOverchargeTrail()
    {
        if (overchargeVFX != null)
        {
            overchargeVFX.StartOverchargeTrail(lastTetheredAnchor);
        }

        if (trailCollider != null)
        {
            trailCollider.EnableCollider();
        }
    }

    private void EndOverchargeTrail()
    {
        if (overchargeVFX != null)
        {
            overchargeVFX.EndOverchargeTrail();
        }

        if (trailCollider != null)
        {
            trailCollider.DisableCollider();
        }
    }

    private void HandleEnemyHit(GameObject enemy)
    {
        Health enemyHealth = enemy.GetComponent<Health>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDmg(trailDamage);
        }
    }

    // Returns true if the player can tether (Not in cooldown state)
    public bool CanTether()
    {
        return !isInCooldown;
    }

    // Gets the last tetherd anchor type (Fire, Ice, Wind)
    public System.Type GetLastAnchorType()
    {
        return lastTetheredAnchor != null ? lastTetheredAnchor.GetType() : null;
    }

    // use method below for respawn/ death of player or debugging
    public void ResetOvercharge()
    {
        currentChargeTime = 0f;
        currentCooldownTime = 0f;
        isInCooldown = false;
        isOvercharged = false;
        lastTetheredAnchor = null;

        // Stop trail Effects
        EndOverchargeTrail();

        OnChargeChanged?.Invoke(0f);

        if (playerHUD != null)
        {
            playerHUD.UpdateOverchargeWheel(0f);
        }
    }

}
