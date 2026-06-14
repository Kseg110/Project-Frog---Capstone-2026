using UnityEngine;
using System;

public class PlayerOvercharge : MonoBehaviour
{

    [Header("Overcharge Settings")]
    [SerializeField] private float chargedTime = 20f;
    [SerializeField] private float chargeCooldown = 15f;
    [SerializeField] private float chargeDecayRate = 1f; // Charge decay countdown when not tethered

    [Header("Fire Anchor Effect (DOT)")]
    [SerializeField] private float fireDamage = 4f;
    [SerializeField] private float fireDamageInterval = 3f;

    [Header("Ice Anchor Effect (Slow)")]
    [SerializeField] private float iceSlowPercent = 50f;

    [Header("Wind Anchor Effect (Speed Boost)")]
    [SerializeField] private float windSpeedBoostPercent = 35f;

    [Header("References")]
    [SerializeField] private PlayerAnchor playerAnchor;
    [SerializeField] private UIPlayerHUD playerHUD;
    [SerializeField] private PlayerOverchargeVFX overchargeVFX;
    [SerializeField] private OverchargeTrailCollider trailCollider;
    [SerializeField] private PlayerMovement playerMovement;

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
    private AnchorType currentAnchorType;

    // Overcharge Properties
    public float CurrentChargeTime => currentChargeTime;
    public float ChargeProgress => Mathf.Clamp01(currentChargeTime / chargedTime);
    public float CooldownProgress => isInCooldown ? Mathf.Clamp01(currentCooldownTime / chargeCooldown) : 0f;
    public bool IsInCooldown => isInCooldown;
    public bool IsOvercharged => isOvercharged;
    public AnchorBase LastTetheredAnchor => lastTetheredAnchor;
    public bool IsTrailActive => isOvercharged || isInCooldown;
    public AnchorType CurrentAnchorType => currentAnchorType;

    // Enum for anchor types
    public enum AnchorType
    {
        None,
        Fire,
        Ice,
        Wind
    }

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
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        currentChargeTime = 0f;
        currentCooldownTime = 0f;
        isInCooldown = false;
        isOvercharged = false;
        currentAnchorType = AnchorType.None;

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

        // Determine anchor type
        currentAnchorType = GetAnchorTypeFromBase(lastTetheredAnchor);

        // Untether Player
        playerAnchor.ReleaseTether();

        // Apply anchor-specific effects
        ApplyAnchorEffect();

        // Start trail effects
        StartOverchargeTrail();

        // Begin Cooldown
        StartCooldown();

        // Notify Listeners
        OnOverchargeActivated?.Invoke();

        Debug.Log($"Overcharge activated! Anchor Type: {currentAnchorType}");
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


        RemoveAnchorEffect();

        currentAnchorType = AnchorType.None;
    }

    private void ApplyAnchorEffect()
    {
        switch (currentAnchorType)
        {
            case AnchorType.Wind:
                ApplyWindSpeedBoost();
                break;
 
        }
    }

    private void RemoveAnchorEffect()
    {
        switch (currentAnchorType)
        {
            case AnchorType.Wind:
                RemoveWindSpeedBoost();
                break;
        }
    }

    private void HandleEnemyHit(GameObject enemy)
    {
        switch (currentAnchorType)
        {
            case AnchorType.Fire:
                ApplyFireEffect(enemy);
                break;
            case AnchorType.Ice:
                ApplyIceEffect(enemy);
                break;

        }
    }

    #region Fire Effect (DOT)
    private void ApplyFireEffect(GameObject enemy)
    {
        // Check if enemy already has OverchargeBurnEffect component
        OverchargeBurnEffect burnEffect = enemy.GetComponent<OverchargeBurnEffect>();
        if (burnEffect == null)
        {
            burnEffect = enemy.AddComponent<OverchargeBurnEffect>();
        }

        // Apply/refresh burn
        burnEffect.ApplyBurn(fireDamage, fireDamageInterval, chargeCooldown);
    }
    #endregion

    #region Ice Effect (Slow)
    private void ApplyIceEffect(GameObject enemy)
    {
        // Check if enemy already has OverchargeSlowEffect component
        OverchargeSlowEffect slowEffect = enemy.GetComponent<OverchargeSlowEffect>();
        if (slowEffect == null)
        {
            slowEffect = enemy.AddComponent<OverchargeSlowEffect>();
        }

        // Apply/refresh slow
        slowEffect.ApplySlow(iceSlowPercent, chargeCooldown);
    }
    #endregion

    #region Wind Effect (Player Speed Boost)
    private void ApplyWindSpeedBoost()
    {
        if (playerMovement != null)
        {
            float speedMultiplier = 1f + (windSpeedBoostPercent / 100f);
            playerMovement.AddSpeedModifier(this, speedMultiplier);
        }
    }

    private void RemoveWindSpeedBoost()
    {
        if (playerMovement != null)
        {
            playerMovement.RemoveSpeedModifier(this);
        }
    }
    #endregion

    private AnchorType GetAnchorTypeFromBase(AnchorBase anchor)
    {
        if (anchor == null) return AnchorType.None;

        if (anchor is AnchorFire)
            return AnchorType.Fire;
        else if (anchor is AnchorIce)
            return AnchorType.Ice;
        else if (anchor is AnchorWind)
            return AnchorType.Wind;
        
        return AnchorType.None;
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
