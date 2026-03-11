using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.5f;
    [Header("Grapple Info")]
    [SerializeField] private bool isTethered;
    private Rigidbody rb;
    private PlayerAnchor playerAnchor;

    private Vector3 moveInput;
    private Vector3 dashDirection;
    private Vector3 lookDirection;

    private bool isDashing;
    private bool movementStoppedExternally;

    private float dashTimer;
    private float dashCooldownTimer;

    public bool IsDashing => isDashing;


    private float currentMaxRadius; // The distance to the tower at this moment
    private Vector3 anchorPosition;
    private readonly float currentMinRadius = 4f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        playerAnchor = GetComponent<PlayerAnchor>(); // cached on awake 
    }

    private void Update()
    {
        UpdateTetherStatus();
        // Update dash cooldown
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (movementStoppedExternally)
            return;

        float horizontalMove = Input.GetAxisRaw("Horizontal");
        float verticalMove = Input.GetAxisRaw("Vertical");

        // No movement input during dash, otherwise create movement vector using horizontalMove and verticalMove
        moveInput = isDashing ? Vector3.zero : new Vector3(horizontalMove, 0f, verticalMove).normalized;

        // Check for valid dash input
        if (!isDashing && dashCooldownTimer <= 0f && Input.GetButtonDown("Jump"))
            StartDash();
    }

    private void FixedUpdate()
    {
        if (movementStoppedExternally)
            return;

        Vector3 moveVector;

        if (isDashing)
        {
            moveVector = dashDirection * (dashDistance / dashDuration) * Time.fixedDeltaTime;
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
                EndDash();
        }
        else
        {
            moveVector = moveInput * moveSpeed * Time.fixedDeltaTime;
        }

        // Apply dynamic shrinking grapple wall
        moveVector = ClampToShrinkingAnchorWall(rb.position, moveVector);

        rb.MovePosition(rb.position + moveVector);

        if (!isDashing && moveInput.sqrMagnitude > 0.0001f)
            transform.forward = moveInput;
    }

    private void UpdateTetherStatus()
    {
        if (playerAnchor != null)
            isTethered = playerAnchor.IsTethered;

        if (isTethered && playerAnchor.CurrentAnchor != null)
        {
            anchorPosition = playerAnchor.CurrentAnchor.transform.position;

            // Shrink currentMaxRadius as player moves closer, but never below currentMinRadius
            float distanceToAnchor = Vector3.Distance(rb.position, anchorPosition);
            if (currentMaxRadius == 0f || distanceToAnchor < currentMaxRadius)
                currentMaxRadius = Mathf.Max(distanceToAnchor, currentMinRadius);
        }
        else
        {
            currentMaxRadius = 0f; // Reset when player is not grappling
        }
    }

    private Vector3 ClampToShrinkingAnchorWall(Vector3 currentPos, Vector3 moveVector)
    {
        if (!isTethered)
            return moveVector;

        Vector3 proposedPos = currentPos + moveVector;
        Vector3 offset = proposedPos - anchorPosition;
        float distance = offset.magnitude;

        // Prevent moving farther than currentMaxRadius
        if (distance > currentMaxRadius)
        {
            Vector3 toCenter = offset.normalized;
            Vector3 tangentMove = moveVector - Vector3.Dot(moveVector, toCenter) * toCenter;

            float overshoot = distance - currentMaxRadius;
            tangentMove *= Mathf.Clamp01(1f - overshoot / moveVector.magnitude);

            return tangentMove;
        }

        // Once inside min radius, block outward movement past it
        Vector3 currentOffset = currentPos - anchorPosition;
        bool insideMinRadius = currentOffset.magnitude < currentMinRadius;

        if (insideMinRadius && distance > currentMinRadius)
        {
            Vector3 toCenter = offset.normalized;
            return moveVector - Vector3.Dot(moveVector, toCenter) * toCenter;
        }

        return moveVector;
    }

    /// <summary>
    /// Stops player movement. 
    /// Intended to be called externally
    /// </summary>
    public void StopMovement(Vector3? forward = null)
    {
        movementStoppedExternally = true;
        moveInput = Vector3.zero;

        if (forward != null) { lookDirection = forward.Value; }
    }

    /// <summary>
    /// Resumes player movement.
    /// Intended to be called externally
    /// </summary>
    public void ResumeMovement()
    {
        movementStoppedExternally = false;
    }

    private void StartDash()
    {
        playerAnchor.ReleaseTether();
        isDashing = true;
        dashTimer = dashDuration;

        // Set the dash direction to the move direction. If there is no move direction, set the dash direction to the direction the player is facing
        dashDirection = moveInput.sqrMagnitude > 0.01f ? moveInput : transform.forward;
    }

    private void EndDash()
    {
        isDashing = false;
        dashCooldownTimer = dashCooldown;
    }
    private void OnDrawGizmos()
    {
        if (!isTethered)
            return;

        // Only draw if we have a valid tower
        if (currentMaxRadius > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(anchorPosition, currentMaxRadius);
        }
    }
}