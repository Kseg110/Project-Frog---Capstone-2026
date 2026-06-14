using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerAnchor))]
public class PlayerMovement : MonoBehaviour, IMovement
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private string hitBoxName = "Hitbox";
    [SerializeField] private float inputSmoothSpeed = 20f;

    private Dictionary<object, float> speedModifiers = new Dictionary<object, float>();
    private float CurrentSpeed
    {
        get
        {
            float finalMult = 1f;
            foreach (var mult in speedModifiers.Values)
                finalMult *= mult;
            return moveSpeed * finalMult;
        }
    }

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("FMod Events")]
    //[SerializeField] private EventReference fireAnchorEvent;
    //[SerializeField] private EventReference iceAnchorEvent;
    //[SerializeField] private EventReference windAnchorEvent;
    [SerializeField] private EventReference dashActivationEvent;

    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction dashAction;

    private bool usingGamepad;

    private Rigidbody rb;

    private PlayerAnchor playerAnchor;
    private UIPlayerHUD playerHUD;
    private CapsuleCollider capsuleCollider;
    private PlayerCrosshair playerCrosshair;

    private Vector3 moveInput;
    private Vector3 dashDirection;
    private Vector3 lookDirection;

    private bool isDashing;
    private bool isMovementStopped;
    private bool isTethered;
    private bool movementStoppedExternally;

    private float dashTimer;
    private float dashCooldownTimer;

    public bool IsDashing => isDashing;
    public float DashCooldownProgress => dashCooldownTimer > 0f ? 1f - (dashCooldownTimer / dashCooldown) : 1f;

    private float currentMaxRadius;
    private Vector3 anchorPosition;
    private readonly float currentMinRadius = 4f;

    private const string GamepadSchemeNameLower = "gamepad";

    private void Awake()
    {
        Transform hitBox = transform.Find(hitBoxName);
        capsuleCollider = hitBox.GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        playerAnchor = GetComponent<PlayerAnchor>();
        playerHUD = FindAnyObjectByType<UIPlayerHUD>();
        playerCrosshair = FindAnyObjectByType<PlayerCrosshair>();

        lookDirection = transform.forward;

        playerInput = GetComponent<PlayerInput>();

        // Desable all action maps at start, will enable the correct one based on input
        foreach (var map in playerInput.actions.actionMaps)
            map.Disable();

        // Enable PlayerMK by default, will switch to Gamepad if input is detected
        playerInput.SwitchCurrentActionMap("PlayerMK");
        SetActionMap("PlayerMK");

        usingGamepad = false;
    }

    private void OnEnable()
    {
        playerInput.onActionTriggered += OnActionTriggered;
        InputSystem.onDeviceChange += OnDeviceChange;
        playerInput.onControlsChanged += OnControlsChanged;
    }

    private void OnDisable()
    {
        playerInput.onActionTriggered -= OnActionTriggered;
        InputSystem.onDeviceChange -= OnDeviceChange;
        playerInput.onControlsChanged -= OnControlsChanged;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (!(device is Gamepad))
            return;

        // Gamepad connected
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected || change == InputDeviceChange.Enabled)
        {
            // Ensure PlayerInput reflects the gamepad control scheme as well as switching map
            if (Gamepad.current != null)
                playerInput.SwitchCurrentControlScheme("Gamepad", Gamepad.current);

            playerInput.SwitchCurrentActionMap("PlayerGamepad");
            SetActionMap("PlayerGamepad");
            usingGamepad = true;
            Debug.Log("Controller connected - switched to PlayerGamepad");
        }

        // Gamepad removed
        if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected || change == InputDeviceChange.Disabled)
        {
            // Switch control scheme back to keyboard+mouse if available
            if (Keyboard.current != null && Mouse.current != null)
                playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current, Mouse.current);

            playerInput.SwitchCurrentActionMap("PlayerMK");
            SetActionMap("PlayerMK");
            usingGamepad = false;
            Debug.Log("Controller removed - switched to PlayerMK");
        }
    }

    // New handler: react to PlayerInput control-scheme changes (fires reliably)
    private void OnControlsChanged(PlayerInput pi)
    {
        var scheme = playerInput.currentControlScheme ?? string.Empty;
        string s = scheme.ToLowerInvariant();
        if (s.Contains(GamepadSchemeNameLower))
        {
            if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != "PlayerGamepad")
            {
                playerInput.SwitchCurrentActionMap("PlayerGamepad");
                SetActionMap("PlayerGamepad");
                usingGamepad = true;
                Debug.Log("ControlsChanged -> switched to PlayerGamepad");
            }
        }
        else
        {
            if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != "PlayerMK")
            {
                playerInput.SwitchCurrentActionMap("PlayerMK");
                SetActionMap("PlayerMK");
                usingGamepad = false;
                Debug.Log("ControlsChanged -> switched to PlayerMK");
            }
        }
    }

    private void OnActionTriggered(InputAction.CallbackContext ctx)
    {
        // Accept Started OR Performed so we don't miss initial inputs from some devices/bindings
        if (!(ctx.phase == InputActionPhase.Started || ctx.phase == InputActionPhase.Performed))
            return;

        var device = ctx.control?.device;

        if (device is Gamepad && playerInput.currentActionMap.name != "PlayerGamepad")
        {
            playerInput.SwitchCurrentActionMap("PlayerGamepad");
            SetActionMap("PlayerGamepad");

            usingGamepad = true;

            Debug.Log("Controller detected via action");
            return;
        }

        if ((device is Keyboard || device is Mouse) && playerInput.currentActionMap.name != "PlayerMK")
        {
            playerInput.SwitchCurrentActionMap("PlayerMK");
            SetActionMap("PlayerMK");

            usingGamepad = false;

            Debug.Log("MK detected via action");
            return;
        }
    }

    private void SetActionMap(string mapName)
    {
        var map = playerInput.actions.FindActionMap(mapName);
        moveAction = map.FindAction("Move");
        lookAction = map.FindAction("Look");
        dashAction = map.FindAction("Dash");
    }

    private void Update()
    {
        UpdateTetherStatus();

        // Fallback auto-switch: if a Gamepad device exists but we haven't switched yet
        if (!usingGamepad && Gamepad.current != null && playerInput.currentActionMap.name != "PlayerGamepad")
        {
            playerInput.SwitchCurrentActionMap("PlayerGamepad");
            SetActionMap("PlayerGamepad");
            usingGamepad = true;
            Debug.Log("Controller auto-switched (Gamepad.current != null)");
        }
        else if (usingGamepad && Gamepad.current == null && playerInput.currentActionMap.name != "PlayerMK")
        {
            // If the connected gamepad was removed, switch back to MK
            playerInput.SwitchCurrentActionMap("PlayerMK");
            SetActionMap("PlayerMK");
            usingGamepad = false;
            Debug.Log("MK auto-switched (no Gamepad.current)");
        }

        // Update dash cooldown
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        float progress = 1f - (dashCooldownTimer / dashCooldown);
        playerHUD?.UpdateDashCooldown(progress);

        if (isMovementStopped || movementStoppedExternally)
            return;

        // READ INPUT
        Vector2 move = moveAction.ReadValue<Vector2>();
        Vector3 rawInput = new Vector3(move.x, 0f, move.y);

        // Clamp magnitude to 1 (keyboard diagonals can exceed it) but preserve analog range for controllers
        if (rawInput.sqrMagnitude > 1f)
            rawInput.Normalize();
        Vector3 targetInput = rawInput.sqrMagnitude > 0.001f ? rawInput : Vector3.zero;

        // Smooth input to prevent analog stick jitter from causing dead-stops
        moveInput = isDashing ? Vector3.zero : Vector3.Lerp(moveInput, targetInput, Time.deltaTime * inputSmoothSpeed);

        //READ LOOK INPUT
        Vector2 look = lookAction.ReadValue<Vector2>();

        // SEND LOOK INPUT TO CROSSHAIR
        if (playerCrosshair != null)
        {
            playerCrosshair.SetControllerMode(usingGamepad);
            playerCrosshair.UpdateControllerLook(look);
        }

        if (usingGamepad && playerCrosshair != null)
        {
            Vector3 dir = playerCrosshair.GetLookDirection();

            if (dir.sqrMagnitude > 0.01f)
            {
                dir.y = 0f;
                lookDirection = dir.normalized;
                rb.MoveRotation(Quaternion.LookRotation(lookDirection));
            }
        }

        // Check for valid dash input
        if (!isDashing && dashCooldownTimer <= 0f && dashAction.WasPressedThisFrame())
            StartDash();
    }

    private void FixedUpdate()
    {
        if (isMovementStopped || movementStoppedExternally)
        {
            rb.MoveRotation(Quaternion.LookRotation(lookDirection));
            return;
        }

        Vector3 moveVector;

        if (isDashing)
        {
            moveVector = dashDirection * (dashDistance / dashDuration) * Time.fixedDeltaTime;
            dashTimer -= Time.fixedDeltaTime;

            CollisionUtility.MoveWithCapsuleCollision(
                rb,
                capsuleCollider,
                moveVector,
                collisionLayers
            );

            if (dashTimer <= 0f)
                EndDash();
        }
        else
        {
            moveVector = moveInput * CurrentSpeed * Time.fixedDeltaTime;
            moveVector = ClampToShrinkingAnchorWall(rb.position, moveVector);

            CollisionUtility.MoveWithCapsuleCollision(
                rb,
                capsuleCollider,
                moveVector,
                collisionLayers
            );

            if (!usingGamepad && moveInput.sqrMagnitude > 0.0001f)
                rb.MoveRotation(Quaternion.LookRotation(moveInput.normalized));
        }
    }
    private void UpdateTetherStatus()
    {
        if (playerAnchor != null)
            isTethered = playerAnchor.IsTethered;

        if (isTethered && playerAnchor.CurrentAnchor != null)
        {
            anchorPosition = playerAnchor.CurrentAnchor.transform.position;
            float distanceToAnchor = Vector3.Distance(rb.position, anchorPosition);
            if (currentMaxRadius == 0f || distanceToAnchor < currentMaxRadius)
                currentMaxRadius = Mathf.Max(distanceToAnchor, currentMinRadius);
        }
        else
        {
            currentMaxRadius = 0f;
        }
    }

    private Vector3 ClampToShrinkingAnchorWall(Vector3 currentPos, Vector3 moveVector)
    {
        if (!isTethered)
            return moveVector;

        Vector3 proposedPos = currentPos + moveVector;
        Vector3 offset = proposedPos - anchorPosition;
        float distance = offset.magnitude;

        if (distance > currentMaxRadius)
        {
            Vector3 toCenter = offset.normalized;
            Vector3 tangentMove = moveVector - Vector3.Dot(moveVector, toCenter) * toCenter;
            float overshoot = distance - currentMaxRadius;
            tangentMove *= Mathf.Clamp01(1f - overshoot / moveVector.magnitude);
            return tangentMove;
        }

        Vector3 currentOffset = currentPos - anchorPosition;
        bool insideMinRadius = currentOffset.magnitude < currentMinRadius;

        if (insideMinRadius && distance > currentMinRadius)
        {
            Vector3 toCenter = offset.normalized;
            return moveVector - Vector3.Dot(moveVector, toCenter) * toCenter;
        }

        return moveVector;
    }

    // Stops player movement. Intended to be called externally.
    public void StopMovement(Vector3? forward = null)
    {
        isMovementStopped = true;
        movementStoppedExternally = true;
        moveInput = Vector3.zero;

        if (forward != null)
            lookDirection = forward.Value;
    }

    // Resumes player movement. Intended to be called externally.
    public void ResumeMovement()
    {
        isMovementStopped = false;
        movementStoppedExternally = false;
    }

    private void StartDash()
    {
        playerAnchor.ReleaseTether();
        isDashing = true;
        dashTimer = dashDuration;
        dashDirection = moveInput.sqrMagnitude > 0.01f ? moveInput : transform.forward;

        RuntimeManager.PlayOneShot(dashActivationEvent, transform.position);

        Debug.Log("start dash");
        PlayerDashVFX.Instance.StartDashVFX();
    }

    private void EndDash()
    {
        isDashing = false;
        dashCooldownTimer = dashCooldown;
        playerHUD?.UpdateDashCooldown(0f);

        Debug.Log("end dash");
        PlayerDashVFX.Instance.EndDashVFX();
    }

    public void AddSpeedModifier(object source, float multiplier)
    {
        if (!speedModifiers.ContainsKey(source))
            speedModifiers.Add(source, multiplier);
    }

    public void RemoveSpeedModifier(object source)
    {
        if (speedModifiers.ContainsKey(source))
            speedModifiers.Remove(source);
    }

    private void OnDrawGizmos()
    {
        if (!isTethered)
            return;

        if (currentMaxRadius > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(anchorPosition, currentMaxRadius);
        }
    }

}