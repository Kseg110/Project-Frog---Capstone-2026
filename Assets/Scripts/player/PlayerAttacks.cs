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

    private float attackWindowTimer = 0f;
    [SerializeField] private float attackWindowDuration = 0.2f; //for reducing player speed with basic shot

    private float fireCooldown => 1f / attacksPerSecond;
    private float lastFireTime = -999f;

    private float chargeTimer;
    private bool isCharging;
    public bool isTethered;

    public float LastChargeValue {  get; private set; }
    public event System.Action<float> OnChargeShotFired;

    public bool isAttacking => isCharging || frogTongue.isActive || attackWindowTimer > 0f; //Public bool to let any other script know if the player is attacking at all.

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
        if (Input.GetButtonDown("Fire1"))
            TryBasicShot();

        if (Input.GetButtonDown("Fire2"))
        {
            if (isTethered)
                StartCharging();
            else
                frogTongue.BeginTongue();
        }

        if (Input.GetButtonUp("Fire2"))
        {
            if (isTethered)
                ReleaseCharging();
            else
                frogTongue.EndTongue();
        }

        if (isCharging)
        {
            chargeTimer += Time.deltaTime;
            chargeTimer = Mathf.Clamp(chargeTimer, 0f, maxChargeTime);
        }

        if (attackWindowTimer > 0f)
            attackWindowTimer -= Time.deltaTime;
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

        LastChargeValue = chargePercent;
        OnChargeShotFired?.Invoke(chargePercent);

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

    private void FireBasicShot()
    {
        attackWindowTimer = attackWindowDuration;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(0f);
        }
    }

    private void TryBasicShot()
    {
        if (!CanShoot()) return;
        if (Time.time < lastFireTime + fireCooldown) return;

        FireBasicShot();
        lastFireTime = Time.time;
    }
}