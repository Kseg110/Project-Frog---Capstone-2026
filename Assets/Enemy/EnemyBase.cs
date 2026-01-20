using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.Processors;

//handles everything commmon to all enemies//
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    #region variables

    [Header("Health")]
    [SerializeField] protected float maxHealth = 100f;
    protected float currentHealth;

    protected bool enableNav = true;
    protected bool isDead = false;

    protected NavMeshAgent agent;

    protected bool canAttack = true;
    [SerializeField] protected GameObject attackHitbox;


    #endregion

    protected virtual void Awake()
    {

        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
     
       
        enableNav = true;
    }
    protected virtual void Update()
    {
        if (isDead) return;

    }
    #region attack
    protected virtual void Attack()
    {
        canAttack = false;
        attackHitbox.SetActive(true);

        StartCoroutine(AttackCooldown());
    }

    protected virtual void AttackEnd()
    {
        canAttack = true;
        attackHitbox.SetActive(false);
    }

    protected virtual IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(2f);
        AttackEnd();
    }
    #endregion
    #region Damage and death
    protected virtual void TakeDmg(float dmg) 
    {
        if(isDead) return;

        currentHealth -= dmg;

       //add event for anim/audio controller to subscribe to

        if (currentHealth <= 0f)
        {
            Die();
        }
    }
    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;
        //trigger death anims
    }

    void IDamageable.TakeDmg(float dmg)
    {
        TakeDmg(dmg);
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
   
}
