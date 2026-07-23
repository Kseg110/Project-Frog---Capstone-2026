using System;
using System.Collections;
using UnityEngine;

// Improved Tether built upon original system created by Kyle and myself by using a Verlet Rope Simulation to allow for true physics interactions on the Tether - draping around objects, affecting Enemies with collisions, etc. -E.M

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class AnchorTether : MonoBehaviour
{
    public event Action OnPointsChanged;

    // These events are for external systems (like upgrades) to react to tethering changes.
    public event Action<AnchorBase> OnAnchorAttached;
    public event Action OnAnchorDetached;

    // Fired when the tether is forcibly severed (e.g. by a TetherBreaker enemy).
    public event Action OnTetherBroken;

    [Header("Tether Transforms")]
    [SerializeField] private Transform startPoint;
    public Transform StartPoint => startPoint;
    [SerializeField] private Transform midPoint;
    public Transform MidPoint => midPoint;
    [SerializeField] private Transform endPoint;
    public Transform EndPoint => endPoint;

    // Anchor currently attached to, or null while detached.
    public AnchorBase CurrentAnchor => currentAnchor;

    [Header("Tether Settings")]
    [Range(8, 60)] public int particleCount = 20;
    public float tetherWidth = 0.1f;
    public float tetherLength = 15f;

    [Header("Verlet Simulation")]
    [SerializeField] private VerletRope rope = new VerletRope();

    [Header("Throw Animation")]
    [Tooltip("How long the rope takes to fly from the player out to the anchor on attach.")]
    [SerializeField] private float throwDuration = 0.5f;
    [Tooltip("Eases the tip's travel from start (0) to anchor (1). Linear is fine; EaseInOut gives a snappier launch + settle.")]
    [SerializeField] private AnimationCurve throwEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Cooldown")]
    [SerializeField] private float tetherCooldown = 1.0f;
    private bool canTether = true;

    [Header("Tether Materials")]
    [SerializeField] private Material fireMaterial;
    [SerializeField] private Material iceMaterial;
    [SerializeField] private Material windMaterial;
    [SerializeField] private Material defaultMaterial;

    private LineRenderer lr;
    private AnchorBase currentAnchor;

    // Probe collider used only as a shape reference for Physics.ComputePenetration. It never needs to be positioned; its world transform is irrelevant.
    private SphereCollider collisionProbe;

    private bool ropeInitialized;

    // Throw animation state.
    private bool isThrowing;
    private Vector3 throwTipPosition;   // current virtual end position while throwing
    private Coroutine throwRoutine;

    // True while the rope is animating its throw from the player out to the anchor.
    // Downstream systems (hitbox rider, restraint, VFX) can gate on this if they want to ignore the rope until it physically lands.
    public bool IsThrowing => isThrowing;

    // Lifecycle
    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.startWidth = lr.endWidth = tetherWidth;
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        EnsureCollisionProbe();
        TryInitializeRope();

        OnAnchorAttached += HandleAnchorMaterial;
        OnAnchorDetached += HandleAnchorDetachedMaterial;
    }

    private void OnDestroy()
    {
        OnAnchorAttached -= HandleAnchorMaterial;
        OnAnchorDetached -= HandleAnchorDetachedMaterial;

        if (collisionProbe != null)
        {
            Destroy(collisionProbe.gameObject);
        }
    }

    private void OnValidate()
    {
        if (!lr) lr = GetComponent<LineRenderer>();
        lr.startWidth = lr.endWidth = tetherWidth;
    }

    private void Update()
    {
        if (IsPrefab) return;

        // Edit-mode preview: the verlet sim only runs at runtime, so just draw a straight line between the points so designers can see the connection.
        if (!Application.isPlaying)
        {
            if (!AreEndPointsValid())
            {
                if (lr) lr.positionCount = 0;
                return;
            }

            lr.enabled = true;
            lr.positionCount = 2;
            lr.SetPosition(0, startPoint.position);
            lr.SetPosition(1, endPoint.position);
            return;
        }

        // Runtime: rope is only visible while attached to something.
        lr.enabled = endPoint != null;

        // Keep the optional MidPoint transform riding the middle of the rope.
        if (midPoint != null && ropeInitialized)
        {
            midPoint.position = GetPointAt(0.5f);
        }
    }

    private void FixedUpdate()
    {
        if (IsPrefab || !Application.isPlaying) return;

        if (startPoint == null)
        {
            if (lr) lr.positionCount = 0;
            ropeInitialized = false;
            return;
        }

        if (!ropeInitialized)
        {
            TryInitializeRope();
            if (!ropeInitialized) return;
        }

        // While throwing, pin the rope's end to the moving virtual tip instead of the real anchor.
        Vector3? pinnedEnd = GetActivePinnedEnd();
        rope.Simulate(Time.fixedDeltaTime, startPoint.position, pinnedEnd, collisionProbe);

        BuildLineFromParticles();
    }

    // Helpers

    private bool AreEndPointsValid() => startPoint != null && endPoint != null;
    private bool IsPrefab => gameObject.scene.rootCount == 0;

    // The world position the sim should pin its end particle to this step: the moving virtual tip while throwing, otherwise the real end point (or null when dangling).
    private Vector3? GetActivePinnedEnd()
    {
        if (isThrowing) return throwTipPosition;
        return endPoint != null ? endPoint.position : (Vector3?)null;
    }

    private void EnsureCollisionProbe()
    {
        if (collisionProbe != null) return;

        var go = new GameObject("TetherCollisionProbe");
        go.hideFlags = HideFlags.HideAndDontSave;

        // Ignore Raycast layer keeps the probe from participating in normal queries.
        go.layer = 2;
        collisionProbe = go.AddComponent<SphereCollider>();
        collisionProbe.isTrigger = true;
    }

    // (Re)builds the particle chain between the current points. Call whenever the tether attaches so the rope doesn't violently snap from a stale shape.
    private void TryInitializeRope()
    {
        if (startPoint == null) return;

        // Dangling rope (no end point): initialize hanging below the start.
        Vector3 endPos = endPoint != null
            ? endPoint.position
            : startPoint.position + Vector3.down * (tetherLength * 0.25f);

        rope.Initialize(startPoint.position, endPos, particleCount, tetherLength);
        ropeInitialized = true;
    }

    private void BuildLineFromParticles()
    {
        if (!lr) lr = GetComponent<LineRenderer>();
        if (lr.positionCount != particleCount) lr.positionCount = particleCount;

        for (int i = 0; i < particleCount; i++)
        {
            lr.SetPosition(i, rope.Positions[i]);
        }
    }

    private void NotifyPointsChanged() => OnPointsChanged?.Invoke();

    // Swaps the LineRenderer's material to match the element of the anchor just attached.
    // Uses lr.material (instance) rather than sharedMaterial so we don't mutate the source asset.
    private void HandleAnchorMaterial(AnchorBase anchor)
    {
        if (lr == null) return;

        Material target = anchor.Element switch
        {
            AnchorElement.Fire => fireMaterial,
            AnchorElement.Ice => iceMaterial,
            AnchorElement.Wind => windMaterial,
            _ => defaultMaterial
        };

        if (target != null)
        {
            lr.material = target;
        }
    }

    // Restores the neutral material when the tether detaches.
    private void HandleAnchorDetachedMaterial()
    {
        if (lr != null && defaultMaterial != null)
        {
            lr.material = defaultMaterial;
        }
    }

    // Public API

    public void SetStartPoint(Transform t, bool instantAssign = false)
    {
        startPoint = t;
        if (instantAssign) TryInitializeRope();
        NotifyPointsChanged();
    }

    public void SetMidPoint(Transform t, bool instantAssign = false)
    {
        midPoint = t;
        NotifyPointsChanged();
    }

    public void SetEndPoint(Transform t, bool instantAssign = false)
    {
        Debug.Log("<color=orange>[AnchorTether]</color> SetEndPoint called with: " + (t ? t.name : "NULL"));

        if (!canTether && t != endPoint && t != null)
        {
            Debug.Log("<color=red>[AnchorTether]</color> BLOCKED by cooldown");
            return;
        }

        endPoint = t;

        AnchorBase newAnchor = null;
        if (endPoint != null)
            newAnchor = endPoint.GetComponentInParent<AnchorBase>();

        Debug.Log("<color=yellow>[AnchorTether]</color> newAnchor = " + (newAnchor ? newAnchor.name : "NULL"));

        if (newAnchor != currentAnchor)
        {
            Debug.Log("<color=cyan>[AnchorTether]</color> Anchor changed!");

            if (currentAnchor != null && newAnchor == null)
            {
                Debug.Log("<color=magenta>[AnchorTether]</color> OnAnchorDetached invoked");
                OnAnchorDetached?.Invoke();
            }
            else if (newAnchor != null)
            {
                Debug.Log("<color=green>[AnchorTether]</color> OnAnchorAttached invoked");
                OnAnchorAttached?.Invoke(newAnchor);
                StartCoroutine(TetherCooldownRoutine());
            }

            currentAnchor = newAnchor;

            // New attachment target: launch the throw so the rope flies out to the anchor.
            // Detach / dangling falls back to the normal rebuild.
            if (Application.isPlaying)
            {
                if (newAnchor != null && endPoint != null)
                    StartThrow(endPoint.position);
                else
                {
                    StopThrow();
                    TryInitializeRope();
                }
            }
        }

        // Guard the instant-rebuild so it doesn't stomp a throw started this same frame (PlayerAnchor.StartTether calls SetEndPoint with instantAssign: true).
        if (instantAssign && Application.isPlaying && !isThrowing)
        {
            TryInitializeRope();
        }

        NotifyPointsChanged();
    }

    private IEnumerator TetherCooldownRoutine()
    {
        canTether = false;
        yield return new WaitForSeconds(tetherCooldown);
        canTether = true;
    }

    // Launches (or restarts) the throw animation toward a target world position.
    // Cancels any in-flight throw so rapid re-attaches don't stack coroutines.
    private void StartThrow(Vector3 targetWorldPos)
    {
        if (throwRoutine != null) StopCoroutine(throwRoutine);
        throwRoutine = StartCoroutine(ThrowRoutine(targetWorldPos));
    }

    // Immediately cancels any active throw (e.g. on detach mid-flight) so the rope doesn't keep flying toward a stale target.
    private void StopThrow()
    {
        if (throwRoutine != null)
        {
            StopCoroutine(throwRoutine);
            throwRoutine = null;
        }
        isThrowing = false;
    }

    // Animates the rope's end from the player out to the anchor over throwDuration.
    // Seeds the chain collapsed at the start so the rope visibly extends outward rather than popping in fully formed.
    private IEnumerator ThrowRoutine(Vector3 targetWorldPos)
    {
        isThrowing = true;

        // Collapse the whole chain onto the start point so the rope launches from the player.
        Vector3 startPos = startPoint.position;
        rope.Initialize(startPos, startPos, particleCount, tetherLength);
        ropeInitialized = true;
        throwTipPosition = startPos;

        float t = 0f;
        while (t < throwDuration)
        {
            t += Time.deltaTime;
            float k = throwEase.Evaluate(Mathf.Clamp01(t / throwDuration));

            // Track the live anchor position so a moving anchor is still hit correctly.
            Vector3 liveTarget = endPoint != null ? endPoint.position : targetWorldPos;
            throwTipPosition = Vector3.Lerp(startPoint.position, liveTarget, k);
            yield return null;
        }

        // Arrived: pin exactly to the anchor and let the normal sim take over.
        throwTipPosition = endPoint != null ? endPoint.position : targetWorldPos;
        isThrowing = false;
        throwRoutine = null;
    }

    // Forcibly severs the tether. Detaches the end point, fires OnTetherBroken (for VFX/SFX/gameplay reactions), then the normal OnAnchorDetached chain so downstream systems (rider, restraint) clean up automatically.
    // The cooldown is applied so the player can't instantly re-attach.
    public void BreakTether()
    {
        if (endPoint == null) return;   // already detached

        Debug.Log("<color=red>[AnchorTether]</color> Tether BROKEN");

        OnTetherBroken?.Invoke();

        // Detach through the normal path so all existing cleanup fires (currentAnchor cleared, OnAnchorDetached raised, cooldown started).
        SetEndPoint(null, true);

        // Force the cooldown - normal SetEndPoint(null) doesn't trigger it because the cooldown is designed to gate NEW attaches, and detach isn't restricted. On a forced break, we want that grace period.
        if (!canTether) return;   // already cooling
        StartCoroutine(TetherCooldownRoutine());
    }

    // World-space pull the rope exerted on the start pin (player) this physics step, expressed in meters of constraint violation. Zero while slack.
    public Vector3 StartTension =>
        Application.isPlaying && ropeInitialized ? rope.StartTension : Vector3.zero;

    // Samples the rope at normalized position t (0 = start, 1 = end). Downstream systems (hitbox riders, VFX, upgrades) read the rope through this.
    public Vector3 GetPointAt(float t)
    {
        if (Application.isPlaying && ropeInitialized)
        {
            float f = Mathf.Clamp01(t) * (particleCount - 1);
            int i = Mathf.FloorToInt(f);
            if (i >= particleCount - 1) return rope.Positions[particleCount - 1];
            return Vector3.Lerp(rope.Positions[i], rope.Positions[i + 1], f - i);
        }

        // Edit-mode / uninitialized fallback: straight line.
        if (!AreEndPointsValid()) return Vector3.zero;
        return Vector3.Lerp(startPoint.position, endPoint.position, Mathf.Clamp01(t));
    }
}

// Self-contained verlet rope simulation. No GameObjects, no Rigidbodies - just position arrays and constraint projection. Cannot stretch apart or explode because constraints are enforced by directly correcting positions.
[Serializable]
public class VerletRope
{
    [Tooltip("Downward acceleration applied to every particle.")]
    public float gravity = -9.81f;

    [Tooltip("0 = frictionless swing, 0.05 = heavy damp. Applied to implicit velocity.")]
    [Range(0f, 0.2f)] public float damping = 0.02f;

    [Tooltip("More iterations = stiffer, less stretchy rope. 15-25 is typical.")]
    [Range(1, 50)] public int constraintIterations = 20;

    [Tooltip("Radius of each particle for world collision.")]
    public float collisionRadius = 0.08f;

    [Tooltip("Layers the rope is deflected by (terrain, walls, props - and enemies, if you want the rope to drape over them).")]
    public LayerMask collisionMask;

    public Vector3[] Positions { get; private set; }

    // How hard the rope pulled against the start pin this step, as a position-error vector in meters (world space). Zero while slack. Consumers (e.g. TetherPlayerRestraint) convert this into movement.
    public Vector3 StartTension { get; private set; }

    private Vector3[] prevPositions;

    private int count;
    private float segmentLength;

    private readonly Collider[] overlapResults = new Collider[8];

    public void Initialize(Vector3 start, Vector3 end, int particleCount, float ropeLength)
    {
        count = Mathf.Max(2, particleCount);
        segmentLength = ropeLength / (count - 1);

        Positions = new Vector3[count];
        prevPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Positions[i] = Vector3.Lerp(start, end, i / (float)(count - 1));
            prevPositions[i] = Positions[i];
        }
    }

    /// <param name="pinnedStart">World position the first particle is locked to (player).</param>
    /// <param name="pinnedEnd">World position the last particle is locked to (anchor), or null for a dangling rope.</param>
    /// <param name="probe">Sphere collider used as the shape reference for ComputePenetration. May be null to skip collision.</param>
    public void Simulate(float dt, Vector3 pinnedStart, Vector3? pinnedEnd, SphereCollider probe)
    {
        if (Positions == null || Positions.Length != count) return;

        // Verlet integration (velocity is implicit in position history)
        float gravityStep = gravity * dt * dt;
        for (int i = 0; i < count; i++)
        {
            Vector3 velocity = (Positions[i] - prevPositions[i]) * (1f - damping);
            prevPositions[i] = Positions[i];
            Positions[i] += velocity;
            Positions[i] += Vector3.up * gravityStep;
        }

        // Constraint projection
        for (int iter = 0; iter < constraintIterations; iter++)
        {
            // Pin endpoints first each iteration so corrections propagate outward from the pinned ends.
            Positions[0] = pinnedStart;
            if (pinnedEnd.HasValue)
                Positions[count - 1] = pinnedEnd.Value;

            for (int i = 0; i < count - 1; i++)
            {
                Vector3 delta = Positions[i + 1] - Positions[i];
                float dist = delta.magnitude;
                if (dist < 1e-6f) continue;

                float error = (dist - segmentLength) / dist;
                Vector3 correction = delta * (0.5f * error);

                bool iPinned = (i == 0);
                bool jPinned = pinnedEnd.HasValue && (i + 1 == count - 1);

                if (!iPinned) Positions[i] += jPinned ? correction * 2f : correction;
                if (!jPinned) Positions[i + 1] -= iPinned ? correction * 2f : correction;
            }

            // Interleave a collision pass every few iterations so constraints and collisions converge together instead of fighting each other.
            if (probe != null && (iter & 3) == 3)
            {
                SolveCollisions(probe);
            }
        }

        // Final collision + pin so rendering never shows penetration or a loose end
        if (probe != null) SolveCollisions(probe);

        Positions[0] = pinnedStart;
        if (pinnedEnd.HasValue)
            Positions[count - 1] = pinnedEnd.Value;

        // If the first free particle sits farther from the pin than one segment length AFTER solving, the rope is taut or snagged around geometry and "wants" to drag the pin toward it. Expose that violation as a vector.
        Vector3 toFirst = Positions[1] - pinnedStart;
        float firstDist = toFirst.magnitude;
        float excess = firstDist - segmentLength;
        StartTension = (excess > 0f && firstDist > 1e-6f)
            ? toFirst * (excess / firstDist)
            : Vector3.zero;
    }

    // Pushes particles out of world geometry. Uses Physics.ComputePenetration with a sphere as the "one" collider, which means the OTHER collider can be ANY type: TerrainCollider, concave MeshCollider, primitives - all supported.
    private void SolveCollisions(SphereCollider probe)
    {
        probe.radius = collisionRadius;

        for (int i = 0; i < count; i++)
        {
            // Continuous check: never let a particle CROSS a surface 
            Vector3 from = prevPositions[i];
            Vector3 motion = Positions[i] - from;
            float dist = motion.magnitude;

            if (dist > 1e-6f && Physics.SphereCast(
                    from, collisionRadius, motion / dist, out RaycastHit hit,
                    dist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                // Stop on the surface; small skin offset avoids re-overlap jitter.
                Positions[i] = hit.point + hit.normal * (collisionRadius * 1.01f);
            }

            // Discrete depenetration for resting contact / residual overlap
            int hits = Physics.OverlapSphereNonAlloc(
                Positions[i], collisionRadius, overlapResults,
                collisionMask, QueryTriggerInteraction.Ignore);

            for (int h = 0; h < hits; h++)
            {
                Collider other = overlapResults[h];
                if (other == probe) continue;

                bool overlapped = Physics.ComputePenetration(
                    probe, Positions[i], Quaternion.identity,
                    other, other.transform.position, other.transform.rotation,
                    out Vector3 direction, out float distance);

                if (overlapped)
                {
                    Positions[i] += direction * distance;
                }
            }
        }
    }
}