using UnityEngine;

// Pulls the Player a slight amount towards the point where the rope is making collisions with objects - can be adjusted. -E.M

[RequireComponent(typeof(AnchorTether))]
public class TetherPlayerRestraint : MonoBehaviour
{
    [SerializeField] private AnchorTether tether;

    [Header("Player")]
    [SerializeField] private Rigidbody playerBody;

    [Tooltip("Player hitbox capsule for collision-safe pulling. Optional but recommended.")]
    [SerializeField] private CapsuleCollider playerHitbox;

    [Tooltip("Layers the player collides with while being pulled (walls, terrain).")]
    [SerializeField] private LayerMask collisionLayers;

    [Header("Tension Response")]
    [Range(0f, 1f)]
    [Tooltip("1 = hard leash (full correction every step). Lower = stretchier, more forgiving rope.")]
    [SerializeField] private float pullStrength = 0.8f;

    [Tooltip("Cap on pull speed in m/s so a big snag can't yank the player across the map in one step.")]
    [SerializeField] private float maxPullSpeed = 25f;

    [Tooltip("Ignore tension below this many meters to avoid micro-jitter at max range.")]
    [SerializeField] private float deadzone = 0.02f;

    [Tooltip("Only restrain while attached to an anchor (uses the tether's attach events).")]
    [SerializeField] private bool onlyWhileAttached = true;

    // True while tension is actively pulling the player this step. AI/movement/animation can read this. 
    public bool IsRestraining { get; private set; }

    private bool attached;

    private void Awake()
    {
        if (tether == null) tether = GetComponent<AnchorTether>();
    }

    private void OnEnable()
    {
        tether.OnAnchorAttached += HandleAttached;
        tether.OnAnchorDetached += HandleDetached;
    }

    private void OnDisable()
    {
        tether.OnAnchorAttached -= HandleAttached;
        tether.OnAnchorDetached -= HandleDetached;
    }

    private void HandleAttached(AnchorBase anchor) => attached = true;
    private void HandleDetached() => attached = false;

    private void FixedUpdate()
    {
        IsRestraining = false;

        if (playerBody == null) return;
        if (onlyWhileAttached && !attached) return;

        Vector3 tension = tether.StartTension;
        if (tension.sqrMagnitude <= deadzone * deadzone) return;

        Vector3 pull = tension * pullStrength;

        // Clamp per-step pull so extreme snags reel smoothly instead of teleporting.
        float maxStep = maxPullSpeed * Time.fixedDeltaTime;
        if (pull.sqrMagnitude > maxStep * maxStep)
            pull = pull.normalized * maxStep;

        if (playerHitbox != null)
        {
            CollisionUtility.MoveWithCapsuleCollision(
                playerBody, playerHitbox, pull, collisionLayers);
        }
        else
        {
            playerBody.MovePosition(playerBody.position + pull);
        }

        IsRestraining = true;
    }
}