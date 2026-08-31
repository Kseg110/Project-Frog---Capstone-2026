using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class PoisonTrapPlant : MonoBehaviour
{
    public enum TargetMode
    {
        Player,
        Enemy,
        Both
    }


    [Header("Projectile Activation")]
    [SerializeField] private Collider targetCollider;
    [SerializeField] private LayerMask projectileLayers;
    [SerializeField] private float projectileActivationDistance = 5f;

    [Header("FMod Events")]
    [SerializeField] private EventReference plantActivationEvent;

    [Header("Activation")]
    [SerializeField] private float activeDuration = 60f;
    [SerializeField] private bool active;

    private bool trapActive;
    private float activeTimer;



    [Header("Cooldown")]
    [SerializeField] private float cooldownDuration = 180f;

    private bool coolingDown;
    private float cooldownTimer;



    [Header("DOT Settings")]
    public float damagePerTick = 5f;
    public float tickInterval = 1f;



    [Header("Poison Area")]
    public float radius = 3f;
    public LayerMask affectLayer = ~0;



    [Header("Targets")]
    public TargetMode targetMode = TargetMode.Player;



    [Header("Particles")]
    public Transform particleRoot;

    public bool enableParticlesWhenActive = true;
    public bool particlesAlwaysActive = false;



    private readonly Dictionary<GameObject, float> occupantsNextTick =
        new Dictionary<GameObject, float>();


    [SerializeField] private SphereCollider triggerCollider;

    private ParticleSystem[] particleSystems;



    void Start()
    {
        EnsureTriggerCollider();

        CacheParticleSystems();


        if (particleSystems != null &&
            particleSystems.Length > 0)
        {
            SetParticlesActive(false);
        }
    }



    void Update()
    {
        CheckProjectileActivation();


        if (coolingDown)
        {
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer <= 0f)
            {
                coolingDown = false;

                if (particlesAlwaysActive)
                    SetParticlesActive(true);
            }
        }



        if (trapActive)
        {
            activeTimer -= Time.deltaTime;


            if (activeTimer <= 0f)
            {
                trapActive = false;
                active = false;

                coolingDown = true;
                cooldownTimer = cooldownDuration;


                occupantsNextTick.Clear();


                SetParticlesActive(false);
            }
        }



        if (particlesAlwaysActive)
        {
            // Always active EXCEPT during cooldown.
            SetParticlesActive(!coolingDown);
        }



        if (!trapActive)
            return;



        float now = Time.time;


        List<GameObject> targets =
            new List<GameObject>(occupantsNextTick.Keys);



        foreach (GameObject target in targets)
        {
            if (target == null)
            {
                occupantsNextTick.Remove(target);
                continue;
            }


            if (now >= occupantsNextTick[target])
            {
                ApplyDamageToTarget(target);


                occupantsNextTick[target] =
                    now + tickInterval;
            }
        }
    }



    void ActivateTrap()
    {
        if (trapActive || coolingDown)
            return;


        trapActive = true;

        active = true;

        activeTimer = activeDuration;


        if (enableParticlesWhenActive)
        {
            SetParticlesActive(true);
        }
    }



    void EnsureTriggerCollider()
    {
        if (triggerCollider == null)
        {
            triggerCollider =
                GetComponent<SphereCollider>();
        }


        if (triggerCollider == null)
        {
            triggerCollider =
                gameObject.AddComponent<SphereCollider>();
        }


        triggerCollider.isTrigger = true;


        // Half size because SphereCollider diameter = radius * 2
        triggerCollider.radius =
            Mathf.Max(0.01f, radius * 0.5f);
    }
    void CacheParticleSystems()
    {
        if (particleRoot != null)
        {
            particleSystems =
                particleRoot.GetComponentsInChildren<ParticleSystem>(true);
        }
        else
        {
            particleSystems =
                GetComponentsInChildren<ParticleSystem>(true);
        }
    }



    public void SetParticlesActive(bool state)
    {
        if (particleSystems == null)
            return;


        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;


            GameObject obj = ps.gameObject;


            if (state)
            {
                if (!obj.activeSelf)
                    obj.SetActive(true);


                if (!ps.isPlaying)
                    ps.Play(true);
            }
            else
            {
                if (ps.isPlaying)
                {
                    ps.Stop(
                        true,
                        ParticleSystemStopBehavior.StopEmitting
                    );
                }


                obj.SetActive(false);
            }
        }
    }



    void CheckProjectileActivation()
    {
        if (targetCollider == null)
            return;


        Collider[] hits =
            Physics.OverlapSphere(
                targetCollider.bounds.center,
                projectileActivationDistance,
                projectileLayers
            );


        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;


            float distance =
                Vector3.Distance(
                    hit.ClosestPoint(targetCollider.bounds.center),
                    targetCollider.bounds.center
                );


            if (distance <= projectileActivationDistance)
            {
                coolingDown = true;
                cooldownTimer = cooldownDuration;
                return;
            }
        }
    }



    void OnTriggerEnter(Collider other)
    {
        // ONLY affect layers entering this poison trigger activate it

        int mask = 1 << other.gameObject.layer;


        if ((affectLayer.value & mask) != 0)
        {
            ActivateTrap();

            RuntimeManager.PlayOneShot(plantActivationEvent, transform.position);
        }
    }



    void OnCollisionEnter(Collision collision)
    {
        // Projectile activation handled by CheckProjectileActivation()
        // No projectile uses this trigger collider
    }



    void OnTriggerStay(Collider other)
    {
        if (!trapActive)
            return;


        if (!IsInAffectLayer(other.gameObject))
            return;


        if (!IsValidTarget(other))
            return;


        GameObject root =
            other.transform.root.gameObject;


        if (!occupantsNextTick.ContainsKey(root))
        {
            occupantsNextTick[root] = Time.time;
        }
    }



    void OnTriggerExit(Collider other)
    {
        GameObject root =
            other.transform.root.gameObject;


        if (occupantsNextTick.ContainsKey(root))
        {
            occupantsNextTick.Remove(root);
        }
    }



    bool IsInAffectLayer(GameObject go)
    {
        int mask = 1 << go.layer;

        return (affectLayer & mask) != 0;
    }



    bool IsValidTarget(Collider col)
    {
        if ((targetMode == TargetMode.Player ||
             targetMode == TargetMode.Both)
             &&
             col.CompareTag("Player"))
        {
            return true;
        }



        if (targetMode == TargetMode.Enemy ||
            targetMode == TargetMode.Both)
        {
            if (col.CompareTag("Enemy"))
                return true;


            if (col.GetComponentInParent<EnemyBase>() != null)
                return true;


            if (col.GetComponentInParent<Health>() != null &&
                !col.CompareTag("Player"))
            {
                return true;
            }
        }


        return false;
    }



    void ApplyDamageToTarget(GameObject targetRoot)
    {
        if (targetRoot == null)
            return;



        if (targetMode == TargetMode.Player ||
            targetMode == TargetMode.Both)
        {
            if (targetRoot.CompareTag("Player"))
            {
                Health hp =
                    targetRoot.GetComponentInChildren<Health>()
                    ??
                    targetRoot.GetComponent<Health>();


                if (hp != null)
                {
                    hp.TakeDmg(damagePerTick);
                    return;
                }
            }
        }



        if (targetMode == TargetMode.Enemy ||
            targetMode == TargetMode.Both)
        {
            EnemyBase enemy =
                targetRoot.GetComponentInChildren<EnemyBase>()
                ??
                targetRoot.GetComponent<EnemyBase>();


            if (enemy != null)
            {
                if (enemy is IDamageable damageable)
                {
                    damageable.TakeDmg(damagePerTick);
                    return;
                }


                EnemyHealth enemyHealth =
                    enemy.GetComponent<EnemyHealth>()
                    ??
                    enemy.GetComponentInChildren<EnemyHealth>();


                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damagePerTick);
                    return;
                }


                Health hp =
                    enemy.GetComponent<Health>()
                    ??
                    enemy.GetComponentInParent<Health>();


                if (hp != null)
                {
                    hp.TakeDmg(damagePerTick);
                    return;
                }
            }



            if (targetRoot.TryGetComponent<IDamageable>
                (out var damageTarget))
            {
                damageTarget.TakeDmg(damagePerTick);
                return;
            }



            Health fallback =
                targetRoot.GetComponentInChildren<Health>()
                ??
                targetRoot.GetComponent<Health>();


            if (fallback != null)
            {
                fallback.TakeDmg(damagePerTick);
            }
        }
    }



    void OnDrawGizmosSelected()
    {
        Gizmos.color =
            new Color(0.2f, 0.8f, 0.2f, 0.15f);


        Gizmos.DrawSphere(
            transform.position,
            radius
        );


        Gizmos.color =
            new Color(0.2f, 1f, 0.2f, 1f);


        Gizmos.DrawWireSphere(
            transform.position,
            radius
        );
    }
}