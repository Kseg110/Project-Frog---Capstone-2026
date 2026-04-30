using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private Healthbar healthbar;

    public bool IsDead { get; private set; }

    private void Awake()
    {
        healthbar = GetComponentInChildren<Healthbar>();

        if (healthbar == null)
        {
            Debug.LogError(
                $"Health on {gameObject.name} requires a child HealthBar.", this);
        }

        currentHealth = maxHealth;
        IsDead = false;
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

        if (currentHealth == 0f)
        {
            Die();
        }
    }
    public void Heal(float amount)
    {
        Debug.Log("heal");
        if (IsDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        healthbar.UpdateHealthBar(maxHealth, currentHealth);
    }

    private void Die()
    {
        IsDead = true;
        Destroy(gameObject);
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