using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private Healthbar healthbar;
    private UIPlayerHUD UIPlayerHUD;

    public bool IsDead { get; private set; }

    private void Awake()
    {
        healthbar = GetComponentInChildren<Healthbar>();

        if (healthbar == null)
        {
            Debug.LogError(
                $"Health on {gameObject.name} requires a child HealthBar.", this);
        }

        UIPlayerHUD = FindAnyObjectByType<UIPlayerHUD>();

        currentHealth = maxHealth;
        IsDead = false;

        UIPlayerHUD?.UpdateHealth(1f);
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
        UIPlayerHUD?.UpdateHealth(currentHealth / maxHealth);

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
        UIPlayerHUD?.UpdateHealth(currentHealth / maxHealth);
    }

    private void Die()
    {
        IsDead = true;
        Destroy(gameObject);
    }
}