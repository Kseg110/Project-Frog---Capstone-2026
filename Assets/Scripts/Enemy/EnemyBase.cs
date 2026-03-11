using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour, IMovement, IDamageable
{
    [Header("References")]
    [SerializeField] private protected Health health;
    private bool isActive;
    private Rigidbody rb;
    private Dictionary<object, float> speedModifiers = new Dictionary<object, float>();
    protected Transform player;
    public Transform Player => player;

    protected bool isNavEnabled = true;

    // ===== AI settings =====
    [Header("Movement")]
    [SerializeField] protected float Speed = 3.5f;
    [SerializeField] protected float acceleration = 8f;
    [SerializeField] protected float rotationSpeed = 8f;
    [SerializeField] protected float stoppingDistance = 1.5f;

    protected NavMeshAgent agent;
    public NavMeshAgent Agent => agent;

    [Header("Hitbox Settings")]
    [SerializeField] protected BoxCollider attackHitbox;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected bool triggerhit;

    [Header("Attack Settings")]
    [SerializeField] protected bool canAttack = false;
    [SerializeField] protected string attackParamName = "canAttack";   // Animator bool parameter
    [Header("Attack Angle")]
    private Animator animator; // Automatically found in Awake
    [Header("Attack Angle")]
    [SerializeField] protected float attackAngle = 60f;

    [Header("Attack Timing")]
    [SerializeField] protected float attackDuration = 2f;
    [SerializeField] protected float attackCooldown = 4f;

    [SerializeField] protected bool isAttacking = false;
    protected bool attackTriggered = false;
    [SerializeField] protected float attackTimer = 0f;
    [SerializeField] protected float cooldownTimer = 0f;
    [SerializeField] protected float FinalSpeed = 3.5f;
    [SerializeField] private float negetivepush = 1;
    Coroutine slowCoroutine;
    float originalSpeed;
    // ===== Unity Callbacks =====

    public void ApplySlow(float slowMultiplier, float duration)
    {
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);
        slowCoroutine = StartCoroutine(SlowRoutine(slowMultiplier, duration));
    }
    private IEnumerator SlowRoutine(float slowMultiplier, float duration)
    {
        AddSpeedModifier(this, slowMultiplier);
        yield return new WaitForSeconds(duration);
        RemoveSpeedModifier(this);
        slowCoroutine = null;
    }
    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        FinalSpeed = Speed;
        agent.speed = FinalSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = false;
        agent.autoBraking = true;
        rb = GetComponent <Rigidbody>();

        if (health == null)
        {
            health = GetComponent<Health>();   
            if (health == null)
            {
                Debug.LogWarning($"No Health component found on {gameObject.name}. Adding one automatically.");
                health = gameObject.AddComponent<Health>();
            }
        }

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (attackHitbox != null)
            attackHitbox.isTrigger = true;
    }

    protected virtual void Update()
    {
        if (health != null && health.IsDead) return;
        if (player == null) return;
        HandleAttackTimers();
        CheckAttackCondition();
        agent.speed = FinalSpeed;

        UpdateHitBox();
        //EnforcePlayerDistance();
        MoveTo(player.position);

    }

    // ===== Movement =====
    protected void MoveTo(Vector3 position)
    {
        if (!isNavEnabled) return;
        if (agent == null) return;

        float distance = Vector3.Distance(transform.position, position);

        if (distance > stoppingDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(position);
            RotateTowardsMovement();
        }
        else
        {
            StopMovement();
        }
    }
    public void Activate(Transform playerTransform)
    {
        player = playerTransform;
        isActive = true;
        if (rb != null) rb.isKinematic = true;
    }

    public virtual void ResumeMovement()
    {
        if (!isNavEnabled) return;
        agent.isStopped = false;
    }

    protected void StopMovement()
    {
        if (!isNavEnabled) return;
        if (agent != null)
            agent.isStopped = true;
    }

    public void AddSpeedModifier(object source, float multiplier)
    {
        if (!speedModifiers.ContainsKey(source))
        {
            speedModifiers.Add(source, multiplier);
            RecalculateSpeed();
        }
    }
    public void RemoveSpeedModifier(object source)
    {
        if (speedModifiers.ContainsKey(source))
        {
            speedModifiers.Remove(source);
            RecalculateSpeed();
        }
    }

    private void RecalculateSpeed()
    {
        float finalMult = 1f;
        foreach (var mult in speedModifiers.Values)
            finalMult *= mult;

        Speed = FinalSpeed * finalMult; // ties into new version's Speed field
        agent.speed = Speed;
    }

    private void RotateTowardsMovement()
    {
        if (agent.velocity.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    // ===== Hitbox =====
    private void UpdateHitBox()
    {
        if (attackHitbox == null) return;

        attackHitbox.enabled = triggerhit;

        if (triggerhit)
        {
            attackHitbox.center = new Vector3(0f, 0f, attackRange / 2f);
            attackHitbox.size = new Vector3(
                attackHitbox.size.x,
                attackHitbox.size.y,
                attackRange
            );
        }
    }
    void IDamageable.TakeDmg(float dmg)
    {
        if (health != null)
            health.TakeDmg(dmg);
    }

    public void StartHit() => triggerhit = true;
    public void EndHit() => triggerhit = false;

    // ===== Attack Condition =====
    private void CheckAttackCondition()
    {

        if (player == null) return;
        // Countdown cooldown
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer < 0f) attackTimer = 0f;
        }
        float distance = Vector3.Distance(transform.position, player.position);
        Vector3 dir = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);

        // Check if within attack range AND facing the player
        bool inRange = distance <= attackRange;
        bool inAngle = angle <= attackAngle * 0.5f;

        if (inRange && inAngle && cooldownTimer <= 0f && !isAttacking)
        {
            // Start attack
            isAttacking = true;
            canAttack = true;
            attackTimer = attackDuration;
            FinalSpeed = Speed * 0.1f;


        }
        if (attackTimer <= 0f && cooldownTimer >= 0f)
        {

            FinalSpeed = Speed;
            // Attack finished
            isAttacking = false;
            canAttack = false;

            // Resume movement
            if (agent != null)
                agent.isStopped = false;
        }
    }

    // ===== Attack Timers =====
    private void HandleAttackTimers()
    {
        // Countdown cooldown
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer < 0f) cooldownTimer = 0f;
        }

        if (animator == null || string.IsNullOrEmpty(attackParamName))
            return;

        // Trigger attack pulse
        if (canAttack && cooldownTimer == 0f && !attackTriggered)
        {
            animator.SetBool(attackParamName, true);
            StartCoroutine(ResetAttackBool());
            cooldownTimer = attackCooldown;
            attackTriggered = true;
        }
    }
    private IEnumerator ResetAttackBool()
    {
        // Short pulse for animator
        yield return new WaitForSeconds(0.001f);
        animator.SetBool(attackParamName, false);
        attackTriggered = false; // allow next attack after cooldown
    }

    //internal void ApplySlow(float slowMultiplier, float slowDuration)
    //{
    //    throw new NotImplementedException();
    //}
    //#endregion

}