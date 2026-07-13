using UnityEngine;

// Allows some Capsule Colliders to hijack the AnchorTether's particles - acting as a "hitbox rider" that moves along the rope and can trigger gameplay effects (damage, knockback, switches). -E.M

[RequireComponent(typeof(AnchorTether))]
public class TetherHitboxRider : MonoBehaviour
{
    [SerializeField] private AnchorTether tether;
    [SerializeField] private GameObject hitboxPrefab;
    [Range(2, 20)][SerializeField] private int hitboxCount = 8;

    [Tooltip("Only activate hitboxes while attached to an anchor (uses OnAnchorAttached/Detached).")]
    [SerializeField] private bool onlyWhileAttached = true;

    private Rigidbody[] bodies;
    private Transform[] hitboxes;
    private bool active;

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

    private void Start()
    {
        bodies = new Rigidbody[hitboxCount];
        hitboxes = new Transform[hitboxCount];

        // If the tether attached BEFORE this Start ran (scene-load attach, or another script's earlier Start calling SetEndPoint), don't stomp that state - ask the tether directly instead of assuming detached.
        active = !onlyWhileAttached || tether.CurrentAnchor != null;

        for (int i = 0; i < hitboxCount; i++)
        {
            GameObject go = Instantiate(hitboxPrefab, transform);
            hitboxes[i] = go.transform;
            bodies[i] = go.GetComponent<Rigidbody>();
            go.SetActive(active);
        }
    }

    private void HandleAttached(AnchorBase anchor) => SetHitboxesActive(true);
    private void HandleDetached() => SetHitboxesActive(!onlyWhileAttached ? active : false);

    private void SetHitboxesActive(bool value)
    {
        active = value;
        if (hitboxes == null) return;
        foreach (Transform t in hitboxes)
            if (t != null) t.gameObject.SetActive(value);
    }

    private void FixedUpdate()
    {
        if (!active || bodies == null) return;

        for (int i = 0; i < hitboxCount; i++)
        {
            float t0 = i / (float)hitboxCount;
            float t1 = (i + 1) / (float)hitboxCount;

            Vector3 a = tether.GetPointAt(t0);
            Vector3 b = tether.GetPointAt(t1);
            Vector3 dir = b - a;

            // MovePosition/MoveRotation on kinematic bodies gives proper swept movement so fast rope swings still register trigger contacts.
            bodies[i].MovePosition((a + b) * 0.5f);
            if (dir.sqrMagnitude > 1e-8f)
                bodies[i].MoveRotation(Quaternion.LookRotation(dir));
        }
    }
}