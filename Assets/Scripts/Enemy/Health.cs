using System;
using System.Collections;
using UnityEngine;
using FMODUnity;

public class Health : MonoBehaviour, IDamageable
{
    [Header("FMod Events")]
    [SerializeField] private EventReference damageTakenEvent;

    private Healthbar healthbar;
    private UIPlayerHUD playerHUD;

    public float maxHealth = 100f;

    private float _currentHealth = 100f;

    public bool IsDead { get; private set; }
    public event Action<GameObject> OnDestroyed;
    
    //Burning DOT
    private bool isBurning;
    private Coroutine burnRoutine;
    private EnemyBase enemy;

    private void Awake()
    {
        healthbar = GetComponentInChildren<Healthbar>();
        enemy = GetComponent<EnemyBase>();

    public event Action<float> OnHealthChanged;

    public float CurrentHealth 
    {
        get => _currentHealth;
        private set
        {
            // Clamp value to valid HP
            float clampedValue = Mathf.Clamp(value, 0, maxHealth);
        if (CompareTag("Player"))
            playerHUD = FindAnyObjectByType<UIPlayerHUD>();
        else
            playerHUD = null;

            // Do nothing if health doesn't change
            if (_currentHealth == clampedValue) return;

            
            _currentHealth = clampedValue;

            OnHealthChanged?.Invoke(_currentHealth);
        }
    }
    private void Awake()
    {
        CurrentHealth = maxHealth;
        if (healthbar == null)
        {
            Debug.LogError($"Health on {gameObject.name} requires a child HealthBar.", this);
        }

        currentHealth = maxHealth;
        IsDead = false;
    }

    // ============================================================
    // BASIC DAMAGE
    // ============================================================
    public void TakeDmg(float dmg)
    {
        if (IsDead) return;

        // Subtract CurrentHealth by damageAmmount
        CurrentHealth -= dmg;

        RuntimeManager.PlayOneShot(damageTakenEvent, transform.position);

        if (CurrentHealth == 0f)
        {
            Die();
        }
    }

    public bool IsMaxHP()
    {
        return CurrentHealth >= maxHealth;
    }

    // ============================================================
    // DAMAGE WITH EFFECT (Burn, Freeze, etc.)
    // ============================================================
    public void TakeDmg(float dmg, string effectType, float effectDuration, float effectValue)
    {
        TakeDmg(dmg);

        if (effectType == "Burn")
            ApplyBurn(effectDuration, effectValue, dmg);
    }

    // ============================================================
    // HEALING
    // ============================================================
    public void Heal(float amount)
    {
        if (IsDead) return;

        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        healthbar.UpdateHealthBar(maxHealth, currentHealth);
        playerHUD?.UpdateHealth(currentHealth / maxHealth);
    }

    // ============================================================
    // DEATH
    // ============================================================
    private void Die()
    {
        IsDead = true;

        if (CompareTag("Player"))
        {
            UIDeathOverlay deathOverlay = FindFirstObjectByType<UIDeathOverlay>();
            if (deathOverlay != null)
                deathOverlay.ShowDeathOverlay();
            else
                Debug.LogError("No PlayerDeathOverlay found in scene.");

            gameObject.SetActive(false);
        }
        else
        {
            OnDestroyed?.Invoke(gameObject);
            Destroy(gameObject);
        }
    }

    // ============================================================
    // BURN LOGIC (Wildfire integrated)
    // ============================================================
    public void ApplyBurn(float duration, float tickRate, float baseDamage)
    {
        if (burnRoutine != null)
            StopCoroutine(burnRoutine);

        burnRoutine = StartCoroutine(BurnRoutine(duration, tickRate, baseDamage));
    }

    private IEnumerator BurnRoutine(float duration, float tickRate, float baseDamage)
    {
        isBurning = true;
        enemy?.SetBurning(true);

        float timer = 0f;

        while (timer < duration)
        {
            float finalTickDamage = baseDamage;

            // Wildfire upgrade (Fire burn damage bonus)
            if (WildfireUpgrade.Instance != null)
            {
                float bonus = WildfireUpgrade.Instance.GetBurnBonus();
                finalTickDamage *= 1f + bonus / 100f;
            }

            TakeDmg(finalTickDamage);

            timer += tickRate;
            yield return new WaitForSeconds(tickRate);
        }

        isBurning = false;
        enemy?.SetBurning(false);
    }
}