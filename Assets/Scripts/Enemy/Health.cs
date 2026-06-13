using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    public float MaxHealth => maxHealth;
    private float currentHealth;
    private Healthbar healthbar;
    private UIPlayerHUD playerHUD;

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
        else
            playerHUD = null;

        if (healthbar == null)
        {
            Debug.LogError($"Health on {gameObject.name} requires a child HealthBar.", this);
        }

        currentHealth = maxHealth;
        IsDead = false;
        playerHUD?.UpdateHealth(currentHealth / maxHealth);
    }

    // ============================================================
    // BASIC DAMAGE
    // ============================================================
    public void TakeDmg(float dmg)
    {
        if (IsDead) return;

        // Subtract currentHealth by damageAmmount
        currentHealth -= dmg;

        // Ensure currentHealth stays between 0 and maxHealth
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // Update healthbar visual
        healthbar.UpdateHealthBar(maxHealth, currentHealth);

        // Update player HUD if this is the player's health
        playerHUD?.UpdateHealth(currentHealth / maxHealth);

        if (currentHealth == 0f)
        {
            Die();
        }
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

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

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