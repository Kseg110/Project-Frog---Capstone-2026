using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.Processors;

// Enemy BaseClass
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] protected Transform player;

    protected bool enableNav = true;
    protected NavMeshAgent agent;

    protected bool canAttack = true;

    [Header("References")]
    [SerializeField] protected GameObject attackHitbox;
    [SerializeField] private protected Health health;

    protected bool isAttacking;
    public bool IsAttacking => isAttacking;



    private bool isActive;
    private Rigidbody rb;

    public void Activate(Transform playerTransform)
    {
        player = playerTransform;
        isActive = true;
        rb.isKinematic = false;
    }
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        // Initialize health component
        if (health == null)
        {
            health = GetComponent<Health>();
            if (health == null)
            {
                Debug.LogWarning($"No Health component found on {gameObject.name}. Adding one automatically.");
                health = gameObject.AddComponent<Health>();

                //// Add Healthbar if missing (required by Health)
                //if (GetComponent<Healthbar>() == null)
                //{
                //    gameObject.AddComponent<Healthbar>();
                //}
            }
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                Debug.Log($"Found player: {player.name}");
            }
            else
            {
                Debug.LogError("No GameObject with 'Player' tag found!");
            }
        }
        enableNav = true;
    }

    protected virtual void Update()
    {
        if (health != null && health.IsDead) return;
    }

    #region Health
    void IDamageable.TakeDmg(float dmg)
    {
        if (health != null)
            health.TakeDmg(dmg);
    }
    // Overload for status effects
    void IDamageable.TakeDmg(float dmg, string effectType, float effectDuration, float effectValue)
    {
        if (health != null)
            health.TakeDmg(dmg);

        if (effectType == "Burn")
        {
            health.ApplyBurn(effectDuration, effectValue, dmg);
        }
        // Add other effects here if needed
    }
    #endregion

    #region Navigation

    public virtual void MoveTo(Vector3 destination)
    {
        if (!enableNav) return;

        agent.isStopped = false;
        agent.SetDestination(destination);

    }
    public virtual void StopMovement()
    {
        if (!enableNav) return;
        agent.isStopped = true;
    }
    public virtual void ResumeMovement()
    {
        if (!enableNav) return;
        agent.isStopped = false;
    }
    #endregion

    // ===============================
    // STATUS EFFECTS FOR UPGRADES
    // ===============================

    public bool IsBurning { get; private set; }
    public bool IsFrozen { get; private set; }
    public bool IsSlowed { get; private set; }

    // Apply a slow effect
    public void ApplySlow(float duration)
    {
        if (IsSlowed) return;
        StartCoroutine(SlowRoutine(duration));
    }

    private IEnumerator SlowRoutine(float duration)
    {
        IsSlowed = true;
        yield return new WaitForSeconds(duration);
        IsSlowed = false;
    }

    // Apply freeze
    public void Freeze(float duration)
    {
        if (IsFrozen) return;
        StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        IsFrozen = true;
        StopMovement(); // freeze movement
        yield return new WaitForSeconds(duration);
        ResumeMovement();
        IsFrozen = false;
    }

    // Burning DOT is handled by Health, but we track the state here
    public void SetBurning(bool state)
    {
        IsBurning = state;
    }

    // Cleanse all effects (used by Extinguisher, Shatter, etc.)
    public void Cleanse()
    {
        IsBurning = false;
        IsFrozen = false;
        IsSlowed = false;
    }

    public void TakeDamagePercent(float percent)
    {
        if (health == null) return;

        // Exemple : percent = 50 → inflige 50% de la vie max
        float damageAmount = (percent / 100f) * health.MaxHealth;

        health.TakeDmg(damageAmount);
    }

    public void TakeDamage(float dmg)
    {
        if (health != null)
            health.TakeDmg(dmg);
    }

    public void TakeDamage(float dmg, string effectType, float effectDuration, float effectValue)
    {
        if (health != null)
            health.TakeDmg(dmg, effectType, effectDuration, effectValue);
    }
}
