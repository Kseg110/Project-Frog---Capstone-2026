
using UnityEngine;

public class Projectile : MonoBehaviour, IProjectile
{
    [SerializeField] protected float baseSpeed = 10f; //Adjust as nessisary 
    [SerializeField] protected float baseDamage = 10f; //Adjust as nessisary 
    [SerializeField] protected float maxScale = 2f; //Adjust as nessisary 

    public float speed;
    public float damage;

    public string effectType;
    public float effectDuration;
    public float effectValue;

    // Wind Upgrade
    private bool isHoming = false;
    private float turnSpeed = 10f; // Adjust as necessary
    private EnemyBase target;
    private float pointBlankRange = 10f;

    // Ice Upgrade
    public bool isPiercingProjectile = false;
    private int pierceCount = 0;

    private IceUpgradeSystem iceSystem;

    private void Awake()
    {
        iceSystem = FindFirstObjectByType<IceUpgradeSystem>();   // a chanlenger pk pas les 2 autre ? meme chose pour le private iceupgrade system
    }

    // Wind homing
    public void EnableHoming(float turnSpeed = 10f)
    {
        isHoming = true;
        this.turnSpeed = turnSpeed;
        target = FindNearestEnemy();
    }

    private EnemyBase FindNearestEnemy()
    {
        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        EnemyBase closest = null;
        float minDist = Mathf.Infinity;

        foreach (var e in enemies)
        {
            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = e;
            }
        }
        return closest;
    }

    public virtual void Initialize(float chargePercent)
    {
        speed = Mathf.Lerp(baseSpeed, baseSpeed * 2f, chargePercent);
        damage = Mathf.Lerp(baseDamage, baseDamage * 3f, chargePercent);

        // FROSTWIND UPGRADE
        float frostBonus = UpgradeManager.Instance.GetTotalStatForElement(
            AnchorElement.Ice,
            UpgradeStat.PrimarySpeed
        );
        speed *= 1f + frostBonus / 100f;

        float scale = Mathf.Lerp(0.25f, maxScale, chargePercent); //Only exists to help visualize the charge's effect on the projectile in the absence of damage
        transform.localScale = Vector3.one * scale;

        Destroy(gameObject, 3f);
    }

    public void Init(float damage, float lifetime)
    {
        this.damage = damage;
        this.speed = baseSpeed;
        Destroy(gameObject, lifetime);
    }

    // -----------------------------
    // MOVEMENT
    // -----------------------------
    protected virtual void Update()
    {
        if (isHoming && target != null)
        {
            Vector3 dir = (target.transform.position - transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    // -----------------------------
    // COLLISION
    // -----------------------------
    private void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            float finalDamage = damage;

            // POINT BLANK BONUS
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < pointBlankRange)
            {
                float bonus = UpgradeManager.Instance.GetTotalStatForElement(
                    AnchorElement.Wind,
                    UpgradeStat.PointBlankDamage
                );
                finalDamage *= 1f + bonus / 100f;
            }

            // APPLY DAMAGE
            if (!string.IsNullOrEmpty(effectType))
                enemy.TakeDamage(finalDamage, effectType, effectDuration, effectValue);
            else
                enemy.TakeDamage(finalDamage);
        }
   
            // -----------------------------
            // ICE UPGRADE SYSTEM HOOK
            // -----------------------------
            if (iceSystem != null)
            {
                bool piercingHit = isPiercingProjectile;
                iceSystem.OnHitEnemy(enemy, piercingHit);       
            }

            // -----------------------------
            // PIERCE LOGIC (Ice secondary)
            // -----------------------------
            if (isPiercingProjectile)
            {
                pierceCount++;

                // Projectile continues infinitely
                return;
            }

            // Destroy normal projectile
            Destroy(gameObject);
    }
}