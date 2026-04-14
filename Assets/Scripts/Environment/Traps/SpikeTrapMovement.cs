using UnityEngine;

/// <summary>
/// Moves an object up and down (or along any axis) in a smooth loop.
/// Attach to the spike trap GameObject and configure in the inspector.
/// </summary>
public class SpikeTrapMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("How far the object moves from its starting position (in units).")]
    [SerializeField] private float moveDistance = 2f;

    [Tooltip("How fast the object completes one full up-down cycle (cycles per second).")]
    [SerializeField] private float speed = 1f;

    [Tooltip("Local axis to move along. Default is up/down (0, 1, 0).")]
    [SerializeField] private Vector3 moveAxis = Vector3.up;

    [Header("Timing")]
    [Tooltip("Delay in seconds before the object starts moving.")]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("Pause duration in seconds at the top and bottom of movement.")]
    [SerializeField] private float pauseAtEnds = 0f;

    [Header("Easing")]
    [Tooltip("If true, the movement eases in and out at each end (smooth). If false, moves at constant speed (linear).")]
    [SerializeField] private bool smoothMotion = true;

    private Vector3 startPosition;
    private float timer;
    private bool isWaiting;
    private float waitTimer;
    private bool atTop;

    private void Start()
    {
        startPosition = transform.localPosition;
        timer = -startDelay * speed; // offset timer to account for start delay
    }

    private void Update()
    {
        // Handle pause at ends
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
                isWaiting = false;
            return;
        }

        timer += Time.deltaTime * speed;

        // Compute 0 → 1 → 0 cycle value
        float t = smoothMotion
            ? (Mathf.Sin(timer * Mathf.PI * 2f - Mathf.PI / 2f) + 1f) / 2f  // smooth sine wave
            : Mathf.PingPong(timer * 2f, 1f);                                 // linear ping-pong

        // Check if we reached an end and should pause
        if (pauseAtEnds > 0f)
        {
            bool nowAtTop = t > 0.99f;
            bool nowAtBottom = t < 0.01f;

            if ((nowAtTop && !atTop) || (nowAtBottom && atTop))
            {
                atTop = nowAtTop;
                isWaiting = true;
                waitTimer = pauseAtEnds;
            }
        }

        // Apply movement along the configured axis
        Vector3 offset = moveAxis.normalized * (t * moveDistance);
        transform.localPosition = startPosition + offset;
    }

    // Editor gizmo to visualize the movement range
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? startPosition : transform.localPosition;

        // Convert to world space for gizmo drawing
        Vector3 worldOrigin = transform.parent != null
            ? transform.parent.TransformPoint(origin)
            : origin;
        Vector3 worldEnd = transform.parent != null
            ? transform.parent.TransformPoint(origin + moveAxis.normalized * moveDistance)
            : origin + moveAxis.normalized * moveDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(worldOrigin, worldEnd);
        Gizmos.DrawWireSphere(worldOrigin, 0.15f);
        Gizmos.DrawWireSphere(worldEnd, 0.15f);
    }
}
