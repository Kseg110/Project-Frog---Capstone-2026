using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private Healthbar healthbar;
    private UIPlayerHUD playerHUD;

    public bool IsDead { get; private set; }
    public event Action<GameObject> OnDestroyed;

    private void Awake()
    {
        healthbar = GetComponentInChildren<Healthbar>();

        if (CompareTag("Player"))
        {
            playerHUD = FindAnyObjectByType<UIPlayerHUD>();
        }
        else
        {
            playerHUD = null;
        }


        if (healthbar == null)
        {
            Debug.LogError(
                $"Health on {gameObject.name} requires a child HealthBar.", this);
        }

        currentHealth = maxHealth;
        IsDead = false;
        playerHUD?.UpdateHealth(currentHealth / maxHealth);
    }

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

    public bool IsMaxHP()
    {
        return currentHealth >= maxHealth;
    }

    public void TakeDmg(float dmg, string effectType, float effectDuration, float effectValue)
    {
        TakeDmg(dmg);
        if (effectType == "Burn")
        {
            ApplyBurn(effectDuration, effectValue, dmg);
        }
    }

    public void Heal(float amount)
    {
        Debug.Log("heal");
        if (IsDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        healthbar.UpdateHealthBar(maxHealth, currentHealth);

        playerHUD?.UpdateHealth(currentHealth / maxHealth);
    }

    private void Die()
    {
        IsDead = true;
        //  so that enemies dont trigger the death overlay and only the player does
        if (CompareTag("Player"))
        {
            UIDeathOverlay deathOverlay = FindFirstObjectByType<UIDeathOverlay>();

            if (deathOverlay != null)
            {
                deathOverlay.ShowDeathOverlay();
            }
            else
            {
                Debug.LogError("No PlayerDeathOverlay found in scene.");
            }

            gameObject.SetActive(false);
        }
        else
        {
            OnDestroyed?.Invoke(gameObject);
            Destroy(gameObject);
        }
    }

    #region Burn DOT
    public void ApplyBurn(float duration, float tickRate, float totalDamage)
    {
        StartCoroutine(BurnCoroutine(duration, tickRate, totalDamage));
    }

    private IEnumerator BurnCoroutine(float duration, float tickRate, float totalDamage)
    {
        float elapsed = 0f;
        int ticks = Mathf.CeilToInt(duration / tickRate);
        float damagePerTick = totalDamage / ticks;

        while (elapsed < duration)
        {
            TakeDmg(damagePerTick);
            yield return new WaitForSeconds(tickRate);
            elapsed += tickRate;
        }
    }
    #endregion
}