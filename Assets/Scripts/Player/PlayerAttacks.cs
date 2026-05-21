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

    //public bool isTethered;
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
    private PlayerChargeAttack playerChargeAttack;
    private PlayerAnchor playerAnchor;
    private PlayerInput playerInput;

    // Input actions
    private InputAction attackAction;          // Fire1 → LMB / Right Trigger
    private InputAction secondaryAttackAction; // Fire2 → RMB / Left Trigger
    private InputAction aimAction;             // Mouse position / Right Stick

    // map tracking + trigger fallbacks
    private string currentActionMapName;
    private float prevAttackValue;
    private float prevSecondaryValue;
    private const float triggerThreshold = 0.5f;

    public bool IsAttacking => isCharging || playerTongueAttack.IsActive || attackWindowTimer > 0f;

    private void Awake()
    {
        mainCamera = Camera.main;
        playerMovement = GetComponent<PlayerMovement>();
        playerTongueAttack = GetComponentInChildren<PlayerTongueAttack>();
        playerChargeAttack = GetComponent<PlayerChargeAttack>();
        playerAnchor = GetComponent<PlayerAnchor>();

        playerTongueAttack.OnTongueFinished += playerMovement.ResumeMovement;

        playerInput = GetComponent<PlayerInput>();
        Debug.Assert(playerInput != null, $"[{gameObject.name}] missing PlayerInput!", this);

        RebindActionsFromCurrentMap();
    }

    private void OnDestroy()
    {
        if (playerTongueAttack != null)
            playerTongueAttack.OnTongueFinished -= playerMovement.ResumeMovement;
    }

    private void RebindActionsFromCurrentMap()
    {
        if (playerInput == null || playerInput.currentActionMap == null)
            return;

        currentActionMapName = playerInput.currentActionMap.name;

        attackAction = playerInput.currentActionMap.FindAction("Attack");
        secondaryAttackAction = playerInput.currentActionMap.FindAction("SecondaryAttack");
        aimAction = playerInput.currentActionMap.FindAction("Look");

        Debug.Assert(attackAction != null, $"[{gameObject.name}] Attack action not found on map {currentActionMapName}!", this);
        Debug.Assert(secondaryAttackAction != null, $"[{gameObject.name}] SecondaryAttack action not found on map {currentActionMapName}!", this);
        Debug.Assert(aimAction != null, $"[{gameObject.name}] Look action not found on map {currentActionMapName}!", this);

        // reset previous values for trigger fallbacks
        prevAttackValue = 0f;
        prevSecondaryValue = 0f;
    }

    private void Update()
    {
        // if the PlayerInput map changed at runtime, rebind to the active map
        if (playerInput != null && playerInput.currentActionMap != null && playerInput.currentActionMap.name != currentActionMapName)
            RebindActionsFromCurrentMap();

        // READ ATTACK INPUTS WITH FALLBACKS FOR TRIGGERS
        bool attackHeld = false;
        bool attackPressedThisFrame = false;

        if (attackAction != null)
        {
            // prefer built-in checks which work when the action is configured as a Button
            attackHeld = attackAction.IsPressed();
            attackPressedThisFrame = attackAction.WasPressedThisFrame();

            // fallback for trigger/axis bindings: treat float values >= threshold as "pressed"
            float val = 0f;
            try { val = attackAction.ReadValue<float>(); } catch { /* ignore if not float */ }

            if (!attackHeld && val >= triggerThreshold)
                attackHeld = true;

            if (!attackPressedThisFrame && val >= triggerThreshold && prevAttackValue < triggerThreshold)
                attackPressedThisFrame = true;

            prevAttackValue = val;
        }

        bool secondaryHeld = false;
        bool secondaryPressedThisFrame = false;
        bool secondaryReleasedThisFrame = false;

        if (secondaryAttackAction != null)
        {
            secondaryHeld = secondaryAttackAction.IsPressed();
            secondaryPressedThisFrame = secondaryAttackAction.WasPressedThisFrame();
            secondaryReleasedThisFrame = secondaryAttackAction.WasReleasedThisFrame();

            float val = 0f;
            try { val = secondaryAttackAction.ReadValue<float>(); } catch { /* ignore if not float */ }

            if (!secondaryHeld && val >= triggerThreshold)
                secondaryHeld = true;

            if (!secondaryPressedThisFrame && val >= triggerThreshold && prevSecondaryValue < triggerThreshold)
                secondaryPressedThisFrame = true;

            if (!secondaryReleasedThisFrame && val < triggerThreshold && prevSecondaryValue >= triggerThreshold)
                secondaryReleasedThisFrame = true;

            prevSecondaryValue = val;
        }

        // Basic shot — held LMB / Right Trigger (rate-limited inside TryBasicShot)
        if (attackHeld)
            TryBasicShot();

        // Secondary — tether / charge / tongue logic
        if (playerAnchor.IsTethered)
        {
            if (secondaryPressedThisFrame)
            {
                if (!playerChargeAttack.IsCharging)
                {
                    playerMovement.StopMovement(GetAimDirection());
                    playerChargeAttack.BeginCharge(playerAnchor.CurrentAnchor);
                }
            }

            if (secondaryHeld)
            {
                playerChargeAttack.UpdateCharge();
            }

            if (secondaryReleasedThisFrame)
            {
                playerChargeAttack.ReleaseCharge(firePoint.position, GetAimDirection());
                playerMovement.ResumeMovement();
            }
        }
        else
        {
            if (secondaryPressedThisFrame)
            {
                TryTongue();
            }

            if (secondaryReleasedThisFrame)
            {
                playerTongueAttack.BeginTongueRetract();
            }
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
        proj.GetComponent<Projectile>().damage = 2f;
    }

    private Vector3 GetAimDirection()
    {
        // Determine if the gamepad stick is actively aiming
        Vector2 aimValue = aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero;
        var lastControl = aimAction != null ? aimAction.activeControl : null;
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