using UnityEngine;
using UnityEngine.InputSystem;

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

    // Input actions
    private InputAction attackAction;          // Fire1 → LMB / Right Trigger
    private InputAction secondaryAttackAction; // Fire2 → RMB / Left Trigger
    private InputAction aimAction;             // Mouse position / Right Stick

    public bool IsAttacking => isCharging || playerTongueAttack.IsActive || attackWindowTimer > 0f;

    private void Awake()
    {
        mainCamera = Camera.main;
        playerMovement = GetComponent<PlayerMovement>();
        playerTongueAttack = GetComponentInChildren<PlayerTongueAttack>();

        playerTongueAttack.OnTongueFinished += playerMovement.ResumeMovement;

        // Cache input actions
        attackAction = InputSystem.actions.FindAction("Attack");
        secondaryAttackAction = InputSystem.actions.FindAction("SecondaryAttack");
        aimAction = InputSystem.actions.FindAction("Look"); // or "Aim" if you renamed it

        Debug.Assert(playerTongueAttack != null, $"[{gameObject.name}] missing PlayerTongueAttack!", this);
    }

    private void OnDestroy()
    {
        if (playerTongueAttack != null)
            playerTongueAttack.OnTongueFinished -= playerMovement.ResumeMovement;
    }

    private void Update()
    {
        // Basic shot — held LMB / Right Trigger (rate-limited inside TryBasicShot)
        if (attackAction.IsPressed()) TryBasicShot();

        // Secondary — RMB / Left Trigger
        if (secondaryAttackAction.WasPressedThisFrame())
        {
            if (isTethered) StartCharging();
            else TryTongue();
        }

        if (secondaryAttackAction.WasReleasedThisFrame())
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

        Vector3 aimDirection = GetAimDirection();
        attackWindowTimer = attackWindowDuration;
        playerMovement.StopMovement(aimDirection);
        Shoot(0f, aimDirection);
        lastFireTime = Time.time;
    }

    private void TryTongue()
    {
        if (isCharging) return;
        playerMovement.StopMovement(GetAimDirection());
        playerTongueAttack.BeginTongueExtend();
    }

    public void StartCharging()
    {
        if (playerTongueAttack.IsActive) return;
        playerMovement.StopMovement(GetAimDirection());
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

    private Vector3 GetAimDirection()
    {
        // Determine if the gamepad stick is actively aiming
        Vector2 aimValue = aimAction.ReadValue<Vector2>();
        var lastControl = aimAction.activeControl;
        bool gamepadActive = lastControl != null
                          && lastControl.device is Gamepad
                          && aimValue.sqrMagnitude > 0.01f;

        if (gamepadActive)
        {
            // Twin-stick: stick (x, y) → world (x, z)
            return new Vector3(aimValue.x, 0f, aimValue.y).normalized;
        }

        // Mouse path
        if (Mouse.current == null)
            return transform.forward;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
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