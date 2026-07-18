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

        if (CompareTag("Player"))
            playerHUD = FindAnyObjectByType<UIPlayerHUD>();

        _currentHealth = maxHealth;
        IsDead = false;
    }

    public event Action<float> OnHealthChanged;

    public float CurrentHealth 
    {
        get => _currentHealth;
        private set
        {
            // Clamp value to valid HP
            float clampedValue = Mathf.Clamp(value, 0, maxHealth);

            // Do nothing if health doesn't change
            if (_currentHealth == clampedValue) return;

            _currentHealth = clampedValue;

            // Update UI
            if (healthbar != null)
                healthbar.UpdateHealthBar(maxHealth, _currentHealth);

            if (playerHUD != null)
                playerHUD.UpdateHealth(_currentHealth / maxHealth);

            OnHealthChanged?.Invoke(_currentHealth);
        }
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



        if (CurrentHealth <= 0f)
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
        else if (effectType == "Freeze")
        {
            if (enemy != null)
                enemy.Freeze(effectDuration);
        }
        else if (effectType == "Slow")
        {
            if (enemy != null)
                enemy.ApplySlow(effectDuration);
        }
    }

    // ============================================================
    // HEALING
    // ============================================================
    public void Heal(float amount)
    {
        if (IsDead) return;

        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);

        if (healthbar != null)
        healthbar.UpdateHealthBar(maxHealth, CurrentHealth);
        if (playerHUD != null)
        playerHUD?.UpdateHealth(CurrentHealth / maxHealth);
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
            Debug.Log("Enemy died");
            enemy.ReleaseSlot();
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