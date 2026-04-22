using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerTongueAttack))]
public class PlayerAttacks : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Attack Settings")]
    [SerializeField] private float attacksPerSecond = 2f;
    [SerializeField] private float attackWindowDuration = 0.5f;
    [SerializeField] private float maxChargeTime = 2f;

    public bool isTethered;
    public float LastChargeValue { get; private set; }
    public event System.Action<float> OnChargeShotFired;

    private float fireCooldown => 1f / attacksPerSecond;
    private float lastFireTime = -999f;
    private float chargeTimer;
    private bool isCharging;
    private float attackWindowTimer;

    private Camera mainCamera;
    private PlayerMovement playerMovement;
    private PlayerTongueAttack playerTongueAttack;

    // True if any attack is active
    public bool IsAttacking => isCharging || playerTongueAttack.IsActive || attackWindowTimer > 0f;

    private void Awake()
    {
        mainCamera = Camera.main;
        playerMovement = GetComponent<PlayerMovement>();
        playerTongueAttack = GetComponentInChildren<PlayerTongueAttack>();

        // Subscribe to event OnTongueFinished to ResumeMovement when tongue fully retracts
        playerTongueAttack.OnTongueFinished += playerMovement.ResumeMovement;

        Debug.Assert(playerTongueAttack != null, $"[{gameObject.name}] missing PlayerTongueAttack!", this);
    }

    private void Update()
    {
        if (Input.GetButton("Fire1")) TryBasicShot();

        if (Input.GetButtonDown("Fire2"))
        {
            if (isTethered) StartCharging();
            else TryTongue();
        }

        if (Input.GetButtonUp("Fire2"))
        {
            if (isTethered) ReleaseCharging();
            else playerTongueAttack.BeginTongueRetract();
        }

        if (isCharging)
            chargeTimer = Mathf.Clamp(chargeTimer + Time.deltaTime, 0f, maxChargeTime);

        // Count down attack window, resume movement when it expires
        if (attackWindowTimer > 0f)
        {
            attackWindowTimer = Mathf.Max(0f, attackWindowTimer - Time.deltaTime);
            if (attackWindowTimer == 0f)
                playerMovement.ResumeMovement();
        }
    }

    private void TryBasicShot()
    {
        if (playerTongueAttack.IsActive) return;
        if (Time.time < lastFireTime + fireCooldown) return;

        Vector3 cursorDirection = GetCursorDirection();
        attackWindowTimer = attackWindowDuration;
        playerMovement.StopMovement(cursorDirection);
        Shoot(0f, cursorDirection);
        lastFireTime = Time.time;
    }

    private void TryTongue()
    {
        if (isCharging) return;
        playerMovement.StopMovement(GetCursorDirection());
        playerTongueAttack.BeginTongueExtend();
    }

    public void StartCharging()
    {
        if (playerTongueAttack.IsActive) return;
        playerMovement.StopMovement(GetCursorDirection());
        isCharging = true;
        chargeTimer = 0f;
    }

    public void ReleaseCharging()
    {
        if (!isCharging) return;
        isCharging = false;
        if (playerTongueAttack.IsActive) return;
        if (Time.time < lastFireTime + fireCooldown) return;

        LastChargeValue = Mathf.Clamp01(chargeTimer / maxChargeTime);
        OnChargeShotFired?.Invoke(LastChargeValue);
        Shoot(LastChargeValue);
        playerMovement.ResumeMovement();
        lastFireTime = Time.time;
    }

    private void Shoot(float chargePercent, Vector3? direction = null)
    {
        Quaternion rotation = direction.HasValue && direction.Value != Vector3.zero
            ? Quaternion.LookRotation(direction.Value)
            : firePoint.rotation;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, rotation);
        proj.GetComponent<Projectile>()?.Initialize(chargePercent);
    }

    private Vector3 GetCursorDirection()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, transform.position);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 direction = ray.GetPoint(distance) - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                return direction.normalized;
        }

        return transform.forward;
    }
}