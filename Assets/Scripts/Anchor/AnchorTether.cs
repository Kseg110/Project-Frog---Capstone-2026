using System;
using System.Collections;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class AnchorTether : MonoBehaviour
{
    public event Action OnPointsChanged;

    // These events are for external systems (like upgrades) to react to tethering changes.
    public event Action<AnchorBase> OnAnchorAttached;
    public event Action OnAnchorDetached;

    [Header("Tether Transforms")]
    [SerializeField] private Transform startPoint;
    public Transform StartPoint => startPoint;
    [SerializeField] private Transform midPoint;
    public Transform MidPoint => midPoint;
    [SerializeField] private Transform endPoint;
    public Transform EndPoint => endPoint;

    [Header("Tether Settings")]
    [Range(2, 100)] public int linePoints = 10;
    public float tetherWidth = 0.1f;
    public float tetherLength = 15f;

    [Header("Physics (spring-damper)")]
    public float stiffness = 350f;
    public float damping = 15f;
    public Vector3 otherPhysicsFactors { get; set; }

    [Header("Rational Bezier Control")]
    [Range(0.25f, 0.75f)] public float midPointPosition = 0.5f;
    [Range(1f, 15f)] public float midPointWeight = 1f;
    private const float EndWeight = 1f;
    private const float StartWeight = 1f;

    // animated midpoint state
    private Vector3 animatedMid;
    private Vector3 velocity;

    private LineRenderer lr;
    private bool isFirstFixed = true;

    // state check (only positions)
    private Vector3 prevStartPos;
    private Vector3 prevEndPos;
    private float prevMidPos;
    private float prevWeight;

    private AnchorBase currentAnchor;
    [SerializeField] private float tetherCooldown = 1.0f;
    private bool canTether = true;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.startWidth = lr.endWidth = tetherWidth;
    }

    private void Start()
    {
        if (!AreEndPointsValid()) return;
        animatedMid = ComputeTargetMidpoint();
        velocity = Vector3.zero;
        RebuildLineImmediate();
    }

    private void OnValidate()
    {
        if (!lr) lr = GetComponent<LineRenderer>();
        lr.startWidth = lr.endWidth = tetherWidth;

        if (!Application.isPlaying && AreEndPointsValid())
        {
            animatedMid = ComputeTargetMidpoint();
            velocity = Vector3.zero;
            RebuildLineImmediate();
        }
    }

    private void Update()
    {
        if (IsPrefab) return;

        if (!AreEndPointsValid())
        {
            if (lr) lr.positionCount = 0;
            return;
        }

        // in editor (not playing) or when endpoints changed - snap and redraw
        if (!Application.isPlaying)
        {
            if (HaveEndpointsOrSettingsChanged())
            {
                animatedMid = ComputeTargetMidpoint();
                velocity = Vector3.zero;
                RebuildLineImmediate();
                NotifyPointsChanged();
            }
        }

        // always update transform of optional MidPoint to match curve
        if (midPoint != null)
        {
            midPoint.position = GetRationalBezierPoint(startPoint.position, animatedMid, endPoint.position, midPointPosition, StartWeight, midPointWeight, EndWeight);
        }

        prevStartPos = startPoint.position;
        prevEndPos = endPoint.position;
        prevMidPos = midPointPosition;
        prevWeight = midPointWeight;
    }

    private void FixedUpdate()
    {
        if (IsPrefab) return;
        if (!AreEndPointsValid()) return;

        if (!isFirstFixed)
        {
            SimulateSpring(Time.fixedDeltaTime);
        }
        isFirstFixed = false;

        BuildLineFromAnimatedMid();
    }

    private bool AreEndPointsValid() => startPoint != null && endPoint != null;
    private bool IsPrefab => gameObject.scene.rootCount == 0;

    private Vector3 ComputeTargetMidpoint()
    {
        Vector3 a = startPoint.position;
        Vector3 b = endPoint.position;

        // base linear midpoint along the line
        Vector3 mid = Vector3.Lerp(a, b, midPointPosition);

        // sag: positive when tetherLength > distance(a,b)
        float dist = Vector3.Distance(a, b);
        float effectiveDist = Mathf.Min(dist, tetherLength);
        float fall = (tetherLength - effectiveDist) / CalculateYFactorAdjustment(midPointWeight);
        mid.y -= fall;
        return mid;
    }

    private float CalculateYFactorAdjustment(float weight)
    {
        // empirical correction to make weight affect sag nonlinearly
        float k = Mathf.Lerp(0.493f, 0.323f, Mathf.InverseLerp(1f, 15f, weight));
        return 1f + k * Mathf.Log(Mathf.Max(weight, 1f));
    }

    private void SimulateSpring(float dt)
    {
        // semi-implicit (symplectic) Euler integration for stability
        Vector3 target = ComputeTargetMidpoint();
        Vector3 springForce = (target - animatedMid) * stiffness; // k * x
        Vector3 dampingForce = -damping * velocity;               // -c * v
        Vector3 accel = springForce + dampingForce + otherPhysicsFactors; // mass = 1
        velocity += accel * dt;
        animatedMid += velocity * dt;

        // snap to target when close and slow
        if (Vector3.Distance(animatedMid, target) < 0.01f && velocity.sqrMagnitude < 0.0001f)
        {
            animatedMid = target;
            velocity = Vector3.zero;
        }
    }

    private Vector3 GetRationalBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t, float w0, float w1, float w2)
    {
        float u = 1f - t;
        float b0 = u * u;
        float b1 = 2f * u * t;
        float b2 = t * t;

        float denom = w0 * b0 + w1 * b1 + w2 * b2;
        // denom should not be zero because weights >= 1 and b0+b1+b2 = 1
        Vector3 num = (w0 * b0) * p0 + (w1 * b1) * p1 + (w2 * b2) * p2;
        return num / denom;
    }

    private void BuildLineFromAnimatedMid()
    {
        if (!lr) lr = GetComponent<LineRenderer>();
        int count = Mathf.Max(2, linePoints) + 1;
        if (lr.positionCount != count) lr.positionCount = count;

        for (int i = 0; i < count - 1; i++)
        {
            float t = i / (float)(count - 1);
            lr.SetPosition(i, GetRationalBezierPoint(startPoint.position, animatedMid, endPoint.position, t, StartWeight, midPointWeight, EndWeight));
        }

        // ensure exact endpoint as last vertex
        lr.SetPosition(count - 1, endPoint.position);
    }

    private void RebuildLineImmediate()
    {
        BuildLineFromAnimatedMid();
    }

    private void NotifyPointsChanged() => OnPointsChanged?.Invoke();

    private bool HaveEndpointsOrSettingsChanged()
    {
        return startPoint.position != prevStartPos
            || endPoint.position != prevEndPos
            || !Mathf.Approximately(midPointPosition, prevMidPos)
            || !Mathf.Approximately(midPointWeight, prevWeight);
    }

    // Public API to set points (instantAssign snaps when true)
    public void SetStartPoint(Transform t, bool instantAssign = false)
    {
        startPoint = t;
        if (instantAssign)
        {
            if (AreEndPointsValid())
            {
                animatedMid = ComputeTargetMidpoint();
                velocity = Vector3.zero;
                RebuildLineImmediate();
            }
            else
            {
                if (lr) lr.positionCount = 0;
            }
        }
        NotifyPointsChanged();
    }

    public void SetMidPoint(Transform t, bool instantAssign = false)
    {
        midPoint = t;
        if (instantAssign)
        {
            if (AreEndPointsValid())
            {
                animatedMid = ComputeTargetMidpoint();
                velocity = Vector3.zero;
                RebuildLineImmediate();
            }
            else
            {
                if (lr) lr.positionCount = 0;
            }
        }
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
            newAnchor = endPoint.GetComponent<AnchorBase>();

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
        }

        if (instantAssign)
        {
            if (AreEndPointsValid())
            {
                animatedMid = ComputeTargetMidpoint();
                velocity = Vector3.zero;
                RebuildLineImmediate();
            }
            else
            {
                if (lr) lr.positionCount = 0;
            }
        }

        NotifyPointsChanged();
    }

    private IEnumerator TetherCooldownRoutine()
    {
        canTether = false;
        yield return new WaitForSeconds(tetherCooldown);
        canTether = true;
    }

    public Vector3 GetPointAt(float t)
    {
        if (!AreEndPointsValid()) return Vector3.zero;
        return GetRationalBezierPoint(startPoint.position, animatedMid, endPoint.position, Mathf.Clamp01(t), StartWeight, midPointWeight, EndWeight);
    }

}
