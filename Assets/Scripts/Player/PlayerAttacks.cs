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
    [SerializeField] public float attacksPerSecond = 2f;
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

    // ============================================================
    // INPUT HANDLING
    // ============================================================
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
        if (playerInput != null &&
            playerInput.currentActionMap != null &&
            playerInput.currentActionMap.name != currentActionMapName)
        {
            RebindActionsFromCurrentMap();
        }

        bool attackHeld = ReadButton(attackAction, ref prevAttackValue, out bool attackPressed);
        bool secondaryHeld = ReadButton(secondaryAttackAction, ref prevSecondaryValue, out bool secondaryPressed, out bool secondaryReleased);

        // PRIMARY ATTACK
        if (attackHeld)
            TryBasicShot();

        // SECONDARY ATTACK
        if (playerAnchor.IsTethered)
        {
            if (secondaryPressed && !playerChargeAttack.IsCharging)
            {
                playerMovement.StopMovement(GetAimDirection());
                playerChargeAttack.BeginCharge(playerAnchor.CurrentAnchor);
            }

            if (secondaryHeld)
                playerChargeAttack.UpdateCharge();

            if (secondaryReleased)
            {
                playerChargeAttack.ReleaseCharge(firePoint.position, GetAimDirection());
                playerMovement.ResumeMovement();
            }
        }
        else
        {
            if (secondaryPressed)
                TryTongue();

            if (secondaryReleased)
                playerTongueAttack.BeginTongueRetract();
        }

        if (isCharging)
            chargeTimer = Mathf.Clamp(chargeTimer + Time.deltaTime, 0f, maxChargeTime);

        if (attackWindowTimer > 0f)
        {
            attackWindowTimer = Mathf.Max(0f, attackWindowTimer - Time.deltaTime);
            if (attackWindowTimer == 0f)
                playerMovement.ResumeMovement();
        }
    }

    // ============================================================
    // PRIMARY ATTACK
    // ============================================================
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

    private void Shoot(float chargePercent, Vector3? direction = null)
    {
        Quaternion rotation = direction.HasValue && direction.Value != Vector3.zero
            ? Quaternion.LookRotation(direction.Value)
            : firePoint.rotation;

        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, rotation);

        var proj = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(chargePercent);
            proj.damage = 2f;
        }

        // ============================================================
        // FAIL-SAFE : IGNORE PLAYER COLLISION FOR PLAYER PROJECTILES
        // ============================================================
        Collider[] projCols = projObj.GetComponentsInChildren<Collider>();
        Collider[] playerCols = GetComponentsInChildren<Collider>();

        foreach (var pCol in projCols)
            foreach (var col in playerCols)
                Physics.IgnoreCollision(pCol, col);
    }

    private void IgnorePlayerCollision(GameObject projObj)
    {
        Collider[] projCols = projObj.GetComponentsInChildren<Collider>();
        if (projCols.Length == 0) return;

        Collider[] playerCols = GetComponentsInChildren<Collider>();

        foreach (var pCol in projCols)
            foreach (var col in playerCols)
                Physics.IgnoreCollision(pCol, col);
    }

    // ============================================================
    // TONGUE ATTACK
    // ============================================================
    private void TryTongue()
    {
        if (isCharging) return;
        playerMovement.StopMovement(GetAimDirection());
        playerTongueAttack.BeginTongueExtend();
    }

    // ============================================================
    // INPUT HELPERS
    // ============================================================
    private bool ReadButton(InputAction action, ref float prevValue, out bool pressed)
    {
        return ReadButton(action, ref prevValue, out pressed, out _);
    }

    private bool ReadButton(InputAction action, ref float prevValue, out bool pressed, out bool released)
    {
        pressed = false;
        released = false;

        if (action == null)
            return false;

        bool held = action.IsPressed();
        pressed = action.WasPressedThisFrame();
        released = action.WasReleasedThisFrame();

        float val = 0f;
        try { val = action.ReadValue<float>(); } catch { }

        if (!held && val >= triggerThreshold)
            held = true;

        if (!pressed && val >= triggerThreshold && prevValue < triggerThreshold)
            pressed = true;

        if (!released && val < triggerThreshold && prevValue >= triggerThreshold)
            released = true;

        prevValue = val;
        return held;
    }

    // ============================================================
    // AIMING
    // ============================================================
    private Vector3 GetAimDirection()
    {
        Vector2 aimValue = aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero;
        var lastControl = aimAction != null ? aimAction.activeControl : null;

        bool gamepadActive =
            lastControl != null &&
            lastControl.device is Gamepad &&
            aimValue.sqrMagnitude > 0.01f;

        if (gamepadActive)
            return new Vector3(aimValue.x, 0f, aimValue.y).normalized;

        if (Mouse.current == null)
            return transform.forward;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        Plane plane = new Plane(Vector3.up, transform.position);

        if (plane.Raycast(ray, out float dist))
        {
            Vector3 dir = ray.GetPoint(dist) - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                return dir.normalized;
        }

        return transform.forward;
    }
}