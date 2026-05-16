using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private InputAction moveAction;
    private InputAction dashAction;

    private Rigidbody rb;
    private PlayerAnchor playerAnchor;
    private UIPlayerHUD playerHUD;
    private CapsuleCollider capsuleCollider;

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

    private void Awake()
    {
        Transform hitBox = transform.Find(hitBoxName);
        capsuleCollider = hitBox.GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        playerAnchor = GetComponent<PlayerAnchor>();
        playerHUD = FindAnyObjectByType<UIPlayerHUD>();
        lookDirection = transform.forward;
        moveAction = InputSystem.actions.FindAction("Move");
        dashAction = InputSystem.actions.FindAction("Dash");
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

        Vector2 move = moveAction.ReadValue<Vector2>();
        Vector3 rawInput = new Vector3(move.x, 0f, move.y);
        // Clamp magnitude to 1 (keyboard diagonals can exceed it) but preserve analog range for controllers
        if (rawInput.sqrMagnitude > 1f)
            rawInput.Normalize();
        Vector3 targetInput = rawInput.sqrMagnitude > 0.001f ? rawInput : Vector3.zero;
        // Smooth input to prevent analog stick jitter from causing dead-stops
        moveInput = isDashing ? Vector3.zero : Vector3.Lerp(moveInput, targetInput, Time.deltaTime * inputSmoothSpeed);

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
            MoveWithCollision(moveVector);
            if (dashTimer <= 0f)
                EndDash();
        }
        else
        {
            moveVector = moveInput * CurrentSpeed * Time.fixedDeltaTime;
            moveVector = ClampToShrinkingAnchorWall(rb.position, moveVector);
            MoveWithCollision(moveVector);

            if (moveInput.sqrMagnitude > 0.0001f)
                rb.MoveRotation(Quaternion.LookRotation(moveInput.normalized));
        }
    }

    private void MoveWithCollision(Vector3 motion)
    {
        int maxIterations = 5;
        Vector3 remaining = motion;

        for (int i = 0; i < maxIterations; i++)
        {
            if (remaining.sqrMagnitude < 0.0001f)
                break;

            Vector3 start = rb.position + capsuleCollider.center + Vector3.up * (capsuleCollider.height / 2 - capsuleCollider.radius);
            Vector3 end = rb.position + capsuleCollider.center - Vector3.up * (capsuleCollider.height / 2 - capsuleCollider.radius);

            if (!Physics.CapsuleCast(start, end, capsuleCollider.radius,
                remaining.normalized, out RaycastHit hit,
                remaining.magnitude, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                rb.MovePosition(rb.position + remaining);
                break;
            }

            float skin = 0.01f;
            float moveDist = Mathf.Max(hit.distance - skin, 0f);

            if (moveDist > 0f)
            {
                Vector3 movePart = remaining.normalized * moveDist;
                rb.MovePosition(rb.position + movePart);
            }

            remaining -= remaining.normalized * moveDist;
            remaining = Vector3.ProjectOnPlane(remaining, hit.normal);
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