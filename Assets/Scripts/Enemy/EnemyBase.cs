using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.Processors;
using UnityEngine.Rendering;

// Enemy BaseClass

[RequireComponent (typeof(MovementComponent))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] protected Transform player;
    [SerializeField] protected MovementComponent movement;
    [SerializeField] protected GameObject attackHitbox;
    [SerializeField] private protected Health health;
    private Rigidbody rb;

    protected NavMeshAgent agent => movement?.Agent;

    protected bool enableNav = true;

    protected bool canAttack = true;

    protected bool isAttacking;
    public bool IsAttacking => isAttacking;

    private bool isActive;

    private float originalAgentSpeed;
    private float environmentalSpeedModifier = 1f;

    // Per-source speed modifiers, stacked multiplicatively (matches PlayerMovement contract)
    private readonly Dictionary<object, float> speedModifiers = new Dictionary<object, float>();

    private Coroutine slowCourotine;
    private Coroutine freezeCoroutine;

    public void Activate(Transform playerTransform)
    {
        player = playerTransform;
        isActive = true;
        rb.isKinematic = false;
    }
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<MovementComponent>();

        if (movement == null)
        {
            Debug.LogError($"{name} is missing a MovementComponent.");
        }

        if (agent != null)
        {
            originalAgentSpeed = agent.speed > 0f ? agent.speed : 3.5f;
        }

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
            GameObject playerObject = GameObject.Find("Player");
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

        UpdateActualSpeed();
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
    //Do not use, use movement component instead//
    public virtual void MoveTo(Vector3 destination)
    {
        if (!enableNav || movement == null) return;

        movement.MoveTo(destination);
    }
    public virtual void StopMovement()
    {
        if (!enableNav || movement == null) return;
        movement.Stop();
    }
    public virtual void ResumeMovement()
    {
        if (!enableNav || movement == null) return;
        movement.SetMovementEnabled(true);
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
        /*
        if (IsSlowed) return;
        StartCoroutine(SlowRoutine(duration));
        */

        if (slowCourotine != null) StopCoroutine(slowCourotine);
        slowCourotine = StartCoroutine(SlowRoutine(duration));
    }

    private IEnumerator SlowRoutine(float duration)
    {
        IsSlowed = true;
        UpdateActualSpeed();

        yield return new WaitForSeconds(duration);

        IsSlowed = false;
        UpdateActualSpeed();
        slowCourotine = null;
    }

    // Apply freeze
    public void Freeze(float duration)
    {
        /*
        if (IsFrozen) return;
        StartCoroutine(FreezeRoutine(duration));
        */

        if (freezeCoroutine != null) StopCoroutine(freezeCoroutine);
        freezeCoroutine = StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        IsFrozen = true;
        UpdateActualSpeed();
        StopMovement(); // freeze movement

        yield return new WaitForSeconds(duration);

        //ResumeMovement();

        IsFrozen = false;
        UpdateActualSpeed();

        if (!isAttacking)
        {
            ResumeMovement();
        }

        freezeCoroutine = null;
    }

    // Burning DOT is handled by Health, but we track the state here
    public void SetBurning(bool state)
    {
        IsBurning = state;
    }

    // Cleanse all effects (used by Extinguisher, Shatter, etc.)
    public void Cleanse()
    {
        if (slowCourotine != null) StopCoroutine(slowCourotine);
        if (freezeCoroutine != null) StopCoroutine(freezeCoroutine);

        IsBurning = false;
        IsFrozen = false;
        IsSlowed = false;

        slowCourotine = null;
        freezeCoroutine = null;

        UpdateActualSpeed();
        ResumeMovement();
    }

    public void TakeDamagePercent(float percent)
    {
        if (health == null) return;

        // Exemple : percent = 50 → inflige 50% de la vie max
        float damageAmount = (percent / 100f) * health.maxHealth;

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

    public void SetEnvironmentalSpeedModifier(float multiplier)
    {
        environmentalSpeedModifier = Mathf.Max(0f, multiplier);
        UpdateActualSpeed();
    }

    private void UpdateActualSpeed()
    {
        if (agent == null) return;

        if (IsFrozen)
        {
            agent.speed = 0f;
            return;
        }

        float currentSlowFactor = IsSlowed ? 0.5f : 1.0f;
        float targetSpeed = originalAgentSpeed * currentSlowFactor * environmentalSpeedModifier;

        if (!Mathf.Approximately(agent.speed, targetSpeed))
        {
            agent.speed = targetSpeed;
        }

        //Debug.Log($"[EnemyBase] {gameObject.name} envMod={environmentalSpeedModifier} orig={originalAgentSpeed} agent.speed={agent.speed} actualVel={agent.velocity.magnitude}");
    }

    // Argument is a MULTIPLIER (0.5 = half speed), stacked multiplicatively.
    // Matches PlayerMovement so a single value behaves identically for both.
    public void AddSpeedModifier(object source, float multiplier)
    {
        if (source == null) return;

        speedModifiers[source] = multiplier;
        RecalculateEnvironmentalSpeed();
    }

    public void RemoveSpeedModifier(object source)
    {
        if (source == null) return;

        if (speedModifiers.Remove(source))
            RecalculateEnvironmentalSpeed();
    }

    private void RecalculateEnvironmentalSpeed()
    {
        float finalMult = 1f;
        foreach (var mult in speedModifiers.Values)
            finalMult *= mult;

        SetEnvironmentalSpeedModifier(finalMult);
    }

    public void ReleaseSlot()
    {
        movement.ReleaseTargetSlot();
    }
}

