using UnityEngine;

public class PlayerAttacks : MonoBehaviour
{
    [Header("Charge Settings")]
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float minChargeTime = 0f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Attack Rate")]
    [SerializeField] private float attacksPerSecond = 2f;
    private float fireCooldown => 1f / attacksPerSecond;
    private float lastFireTime = -999f;

    private float chargeTimer;
    private bool isCharging;

    public bool isAttacking => isCharging || frogTongue.isActive; //Public bool to let any other script know if the player is attacking at all.

    [SerializeField] private PlayerTongueAttack frogTongue;

    private void Awake()
    {
        if (frogTongue == null)
        {
            frogTongue = GetComponentInChildren<PlayerTongueAttack>();
            if (frogTongue == null)
            {
                Debug.LogError("FrogTongue reference not found on this GameObject");
            }
        }
    }

    private bool CanShoot()
    {
        if (frogTongue == null)
            return true;

        return !frogTongue.isActive;
    }

    void Update()
    {
        // -------------------------
        // ATTACK 1 — Tongue
        // -------------------------
        if (frogTongue != null)
        {
            if (Input.GetButtonDown("Fire1"))
                frogTongue.BeginTongue();

            if (Input.GetButtonUp("Fire1"))
                frogTongue.EndTongue();
        }

        // -------------------------
        // ATTACK 2 — Charged Shot
        // -------------------------
        if (Input.GetButtonDown("Fire2"))
            StartCharging();

        if (Input.GetButtonUp("Fire2"))
            ReleaseCharging();

        if (isCharging)
        {
            chargeTimer += Time.deltaTime;
            chargeTimer = Mathf.Clamp(chargeTimer, 0f, maxChargeTime);
        }
    }

    public void StartCharging()
    {
        if (!CanShoot()) return;

        isCharging = true;
        chargeTimer = minChargeTime; 
    }

    public void ReleaseCharging()
    {
        if (!isCharging) return;
        isCharging = false;
        if (!CanShoot()) return;
        if (Time.time < lastFireTime + fireCooldown) return;

        float chargePercent = Mathf.Clamp01(chargeTimer / maxChargeTime);

        FireProjectile(chargePercent);

        lastFireTime = Time.time;
    }

    private void FireProjectile(float chargePercent)
    {
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(chargePercent);
        }
    }
}