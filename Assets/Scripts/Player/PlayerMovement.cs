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
    [SerializeField] private ParticleSystem dashEffect;
    [SerializeField] private float dashEffectBackOffset = 1f;
    [SerializeField] private float dashEffectHeightOffset = 0.5f;
    

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
    private bool isSubToInputManager;

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

        // Enable PlayerMK by default, will switch to Gamepad if input is detected
        foreach (var map in playerInput.actions.actionMaps)
            map.Disable();

        playerInput.SwitchCurrentActionMap("PlayerMK");
        SetActionMap("PlayerMK");
        usingGamepad = false;

        SubToInputManager();
    }

    private void OnEnable()
    {
        SubToInputManager();
    }

    private void OnDisable()
    {
        UnsubFromInputManager();

    }

    private void OnDestroy()
    {
        UnsubFromInputManager();
    }

    private void SubToInputManager()
    {
        if (InputManager.Instance != null && !isSubToInputManager)
        {
            InputManager.Instance.OnInputDeviceChanged += OnInputDeviceChanged;
            isSubToInputManager = true;
            SyncWithInputManager();
        }
    }

    private void UnsubFromInputManager()
    {
        if (InputManager.Instance != null && isSubToInputManager)
        {
            InputManager.Instance.OnInputDeviceChanged -= OnInputDeviceChanged;
            isSubToInputManager = false;
        }
    }

    private void OnInputDeviceChanged(InputManager.InputDevice newDevice)
    {
        bool shouldUseGamepad = (newDevice == InputManager.InputDevice.Gamepad);
        SwitchInputMode(shouldUseGamepad);
        
    }

    private void SyncWithInputManager()
    {
        if (InputManager.Instance == null)
            return;

        bool shouldUseGamepad = InputManager.Instance.IsUsingGamepad();
        SwitchInputMode(shouldUseGamepad);
    }

    private void SwitchInputMode(bool shouldUseGamepad)
    {
        if (shouldUseGamepad == usingGamepad)
            return;

        usingGamepad = shouldUseGamepad;
        string targetMap = usingGamepad ? "PlayerGamepad" : "PlayerMK";

        playerInput.SwitchCurrentActionMap(targetMap);
        SetActionMap(targetMap);

        Debug.Log($"[PlayerMovement] Switched to {targetMap}");
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
            //moveVector = ClampToShrinkingAnchorWall(rb.position, moveVector);

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

    #region OG Shrinking radius
    //-------------------------------//
    // Original shrinking tether radius method //
    //------------------------------//

    //private Vector3 ClampToShrinkingAnchorWall(Vector3 currentPos, Vector3 moveVector)
    //{
    //    if (!isTethered)
    //        return moveVector;

    //    Vector3 proposedPos = currentPos + moveVector;
    //    Vector3 offset = proposedPos - anchorPosition;
    //    float distance = offset.magnitude;

    //    if (distance > currentMaxRadius)
    //    {
    //        Vector3 toCenter = offset.normalized;
    //        Vector3 tangentMove = moveVector - Vector3.Dot(moveVector, toCenter) * toCenter;
    //        float overshoot = distance - currentMaxRadius;
    //        tangentMove *= Mathf.Clamp01(1f - overshoot / moveVector.magnitude);
    //        return tangentMove;
    //    }

    //    Vector3 currentOffset = currentPos - anchorPosition;
    //    bool insideMinRadius = currentOffset.magnitude < currentMinRadius;

    //    if (insideMinRadius && distance > currentMinRadius)
    //    {
    //        Vector3 toCenter = offset.normalized;
    //        return moveVector - Vector3.Dot(moveVector, toCenter) * toCenter;
    //    }

    //    return moveVector;
    //}
    #endregion

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

        // Spawn the trail effect behind the player, facing opposite the dash direction
        Vector3 spawnPosition = transform.position - dashDirection * dashEffectBackOffset + Vector3.up * dashEffectHeightOffset;
        Quaternion spawnRotation = Quaternion.LookRotation(-dashDirection);
        ParticleSystem fx = Instantiate(dashEffect, spawnPosition, spawnRotation);
        fx.Play();

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