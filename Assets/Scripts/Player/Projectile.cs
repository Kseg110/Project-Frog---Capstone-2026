using UnityEngine;
using System.Collections;

public class Projectile : MonoBehaviour, IProjectile
{
    [SerializeField] protected float baseSpeed = 10f;
    [SerializeField] protected float baseDamage = 10f;
    [SerializeField] protected float maxScale = 2f;

    public float speed;
    public float damage;

    public string effectType;
    public float effectDuration;
    public float effectValue;
    public bool isPlayerProjectile = false;

    // Wind Upgrade
    private bool isHoming = false;
    private bool skipAutoHoming = true; // prevents auto homing in awake    
    private float turnSpeed = 10f;
    private EnemyBase target;
    private float pointBlankRange = 10f;

    // Ice Upgrade
    public bool isPiercingProjectile = false;
    private int pierceCount = 0;

    // Default is 0 so basic shots do not apply knockback. Charged attacks will add knockback based on charge time
    [Tooltip("Knockback distance applied to enemies when hit by player projectiles. 0 = no knockback.")]
    public float knockbackDistance = 0f;

    private void Awake()
    {
        // If homing upgrade is active, enable homing with delay
        if (!skipAutoHoming && HomingDartsUpgrade.Instance != null && HomingDartsUpgrade.Instance.IsEnabled())
            EnableHoming();
    }

    public virtual void Initialize(float chargePercent)
    {
        // Base speed & damage scaling
        speed = Mathf.Lerp(baseSpeed, baseSpeed * 2f, chargePercent);
        damage = Mathf.Lerp(baseDamage, baseDamage * 3f, chargePercent);

        // Frostwind (Ice primary speed)
        if (FrostwindUpgrade.Instance != null)
        {
            float bonus = FrostwindUpgrade.Instance.GetBonus();
            speed *= 1f + bonus / 100f;
        }

        // Searing Shot (Fire primary dart damage)
        if (effectType == "Burn" && SearingShotUpgrade.Instance != null)
        {
            float bonus = SearingShotUpgrade.Instance.GetDartBonus();
            damage *= 1f + bonus / 100f;
        }

        // Visual charge scaling
        float scale = Mathf.Lerp(0.25f, maxScale, chargePercent);
        transform.localScale = Vector3.one * scale;

        Destroy(gameObject, 3f);
    }

    public void Init(float damage, float lifetime)
    {
        this.damage = damage;
        this.speed = baseSpeed;
        Destroy(gameObject, lifetime);
    }

    // ============================
    // HOMING LOGIC
    // ============================
    public void EnableHoming(float turnSpeed = 10f)
    {
        isHoming = true;
        this.turnSpeed = turnSpeed;
        target = FindNearestEnemy();
    }

    public void EnableHomingDelayed(float delay, float turnSpeed = 10f)
    {
        StartCoroutine(EnableHomingAfterDelay(delay, turnSpeed));
    }

    private IEnumerator EnableHomingAfterDelay(float delay, float turnSpeed)
    {
        yield return new WaitForSeconds(delay);
        EnableHoming(turnSpeed);
    }

    private EnemyBase FindNearestEnemy()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
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

    // ============================
    // COLLISION
    // ============================
    private void OnTriggerEnter(Collider other)
    {
        // Ignore trigger colliders that aren't enemies or player
        if (other.isTrigger && !other.CompareTag("Enemy") && !other.CompareTag("Player"))
            return;

        // ============================
        // DAMAGE PLAYER IF ENEMY PROJECTILE
        // ============================
        if (!isPlayerProjectile && other.CompareTag("Player"))
        {
            var player = other.GetComponentInParent<PlayerTakeDamage>();
            if (player != null)
            {
                Vector3 knockDir = (other.transform.position - transform.position).normalized;
                knockDir.y = 0f;
                player.TryApplyDamageAndKnockback(damage, knockDir, 5f);
            }

            Destroy(gameObject);
            return;
        }

        // ============================
        // IGNORE PLAYER IF PLAYER PROJECTILE
        // ============================
        if (isPlayerProjectile && other.CompareTag("Player"))
            return;

        // ============================
        // DAMAGE ENEMY IF PLAYER PROJECTILE
        // ============================
        var enemy = other.GetComponent<EnemyBase>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyBase>();

        if (enemy != null && isPlayerProjectile)
        {
            float finalDamage = damage;
            // DEBUG: show if this projectile intended to apply knockback
            Debug.Log($"[Projectile] Hit enemy '{enemy.name}'. isPlayerProjectile={isPlayerProjectile}, knockbackDistance={knockbackDistance}", this);

            // Point Blank Shot
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (PointBlankShotUpgrade.Instance != null && dist < pointBlankRange)
            {
                float bonus = PointBlankShotUpgrade.Instance.GetBonus();
                finalDamage *= 1f + bonus / 100f;
            }

            // Cryo Fragility (bonus if slowed)
            if (CryoFragilityUpgrade.Instance != null && enemy.IsSlowed)
            {
                float bonus = CryoFragilityUpgrade.Instance.GetBonus();
                finalDamage *= 1f + bonus / 100f;
            }

            // Apply damage + effect
            if (!string.IsNullOrEmpty(effectType))
                enemy.TakeDamage(finalDamage, effectType, effectDuration, effectValue);
            else
                enemy.TakeDamage(finalDamage);

            // Apply knockback to enemy only if knockbackDistance > 0
            if (knockbackDistance > 0f)
            {
                Vector3 pushDir = (enemy.transform.position - transform.position);
                pushDir.y = 0f;
                if (pushDir.sqrMagnitude > 0.0001f)
                {
                    pushDir.Normalize();
                    var enemyKnock = enemy.GetComponentInParent<EnemyKnockback>();
                    if (enemyKnock != null)
                    {
                        Debug.Log($"[Projectile] Using EnemyKnockback on '{enemy.name}' (distance={knockbackDistance})", enemy);
                        enemyKnock.ApplyKnockback(pushDir, knockbackDistance);
                    }
                    else
                    {
                        Debug.Log($"[Projectile] EnemyKnockback not found on '{enemy.name}', falling back to Rigidbody/transform nudge", enemy);
                        // fallback: try attached rigidbody
                        var rb = other.attachedRigidbody ?? enemy.GetComponentInParent<Rigidbody>();
                        if (rb != null)
                        {
                            if (rb.isKinematic)
                                rb.MovePosition(rb.position + pushDir * knockbackDistance);
                            else
                                rb.AddForce(pushDir * knockbackDistance, ForceMode.Impulse);
                        }
                        else
                        {
                            // last resort: nudge root transform
                            var root = other.transform.root;
                            root.position += pushDir * knockbackDistance;
                        }
                    }
                }
            }

            // Extinguisher
            if (ExtinguisherUpgrade.Instance != null)
                ExtinguisherUpgrade.Instance.OnHitEnemy(enemy);

            // Shatter
            if (ShatterUpgrade.Instance != null)
                ShatterUpgrade.Instance.OnHitEnemy(enemy);

            // Lethal Piercing
            if (isPiercingProjectile && LethalPiercingUpgrade.Instance != null)
            {
                float bonus = LethalPiercingUpgrade.Instance.GetBonus();
                enemy.TakeDamagePercent(bonus);
            }

            // Piercing logic
            if (isPiercingProjectile)
            {
                pierceCount++;
                return;
            }

            Destroy(gameObject);
            return;
        }

        // Destroy on hitting walls or other objects
        Destroy(gameObject);
    }
}