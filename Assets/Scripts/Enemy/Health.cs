using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    public float maxHealth = 100f;

    private float _currentHealth = 100f;

    public bool IsDead { get; private set; }
    public event Action<GameObject> OnDestroyed;

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

            OnHealthChanged?.Invoke(_currentHealth);
        }
    }
    private void Awake()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
    }

    public void TakeDmg(float dmg)
    {
        if (IsDead) return;

        // Subtract CurrentHealth by damageAmmount
        CurrentHealth -= dmg;

        if (CurrentHealth == 0f)
        {
            Die();
        }
    }

    public bool IsMaxHP()
    {
        return CurrentHealth >= maxHealth;
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
        if (IsDead) return;

        CurrentHealth += amount;
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