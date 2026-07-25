using TMPro;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;


public class PlayerAnchor : MonoBehaviour
{
    [Header("FMod Events")]
    [SerializeField] private EventReference tetherAttachEvent;

    public event Action<AnchorBase> OnTetherStarted;
    public event Action OnTetherReleased;
    public event Action<AnchorBase> OnAnchorChanged;

    private AnchorBase[] allAnchors;
    private AnchorBase currentAnchor;
    private bool isTethered;

    private InputAction tetherAction;
    private PlayerInput playerInput;
    private string currentActionMapName;
    [SerializeField] private AnchorTether anchorTether;
    [SerializeField] private PlayerOvercharge playerOvercharge;

    [Header("Line of Sight")]
    [Tooltip("Geometry that blocks tethering. Should NOT include enemies or the Player's own layer.")]
    [SerializeField] private LayerMask losBlockingMask;
    [Tooltip("Raycast origin offset so the check doesn't start inside the player's own collider or the floor.")]
    [SerializeField] private Vector3 losOriginOffset = new Vector3(0f, 1f, 0f);
    [Tooltip("Shrinks the check slightly so geometry right at the anchor doesn't self-block.")]
    [SerializeField] private float losEndPadding = 0.15f;
    [Tooltip("Use a SphereCast instead of a Raycast so thin gaps (railings, grates) don't read as clear. Disable this first when debugging false blocks.")]
    [SerializeField] private bool useThickCheck = true;
    [SerializeField] private float losThickness = 0.1f;
    [Tooltip("If true, walking behind cover while tethered severs the tether.")]
    [SerializeField] private bool requireLineOfSightWhileTethered = true;

    [Header("LOS Debug")]
    [Tooltip("Logs which collider is blocking each anchor. Spams every frame - turn off when done.")]
    [SerializeField] private bool logLineOfSight = false;

    // The anchor actually attached to. Distinct from currentAnchor, which is only the nearest valid CANDIDATE and gets reassigned every frame.
    private AnchorBase attachedAnchor;

    public bool IsTethered => isTethered;
    public AnchorBase CurrentAnchor => currentAnchor;
    public AnchorBase AttachedAnchor => attachedAnchor;

    private void Awake()
    {
        allAnchors = FindObjectsByType<AnchorBase>(FindObjectsSortMode.None);

        playerInput = GetComponent<PlayerInput>();
        Debug.Assert(playerInput != null, $"[{gameObject.name}] missing PlayerInput!", this);

        if (playerOvercharge == null)
        {
            playerOvercharge = GetComponent<PlayerOvercharge>();
        }

        RebindTetherActionFromCurrentMap();
    }

    private void Update()
    {
        // If the active map changed (PlayerMK <-> PlayerGamepad), rebind tether action to the active map
        if (playerInput != null && playerInput.currentActionMap != null && playerInput.currentActionMap.name != currentActionMapName)
            RebindTetherActionFromCurrentMap();

        UpdateCurrentAnchor();
        HandleInput();
        ValidateTether();
    }

    private void RebindTetherActionFromCurrentMap()
    {
        if (playerInput == null || playerInput.currentActionMap == null)
            return;

        currentActionMapName = playerInput.currentActionMap.name;
        tetherAction = playerInput.currentActionMap.FindAction("Tether");

        Debug.Assert(tetherAction != null, $"Tether action not found on active map '{currentActionMapName}' for [{gameObject.name}]!", this);
    }

    private void UpdateCurrentAnchor()
    {
        currentAnchor = GetClosestAnchorInRange();
    }

    private void HandleInput()
    {
        if (tetherAction != null && tetherAction.WasPressedThisFrame())
        {
            if (isTethered)
                ReleaseTether();
            else
                StartTether();
        }
    }

    private void ValidateTether()
    {
        if (!isTethered)
            return;

        // Anchor destroyed / despawned.
        if (attachedAnchor == null)
        {
            ReleaseTether();
            return;
        }

        // Out of range.
        if (Vector3.Distance(transform.position, attachedAnchor.transform.position) > attachedAnchor.TetherRange)
        {
            ReleaseTether();
            return;
        }

        // Cover moved between player and anchor.
        if (requireLineOfSightWhileTethered && !HasLineOfSight(attachedAnchor))
        {
            ReleaseTether();
        }
    }

    private AnchorBase GetClosestAnchorInRange()
    {
        AnchorBase closest = null;
        float minDist = float.MaxValue;

        foreach (AnchorBase anchor in allAnchors)
        {
            if (anchor == null)
                continue;

            float distance = Vector3.Distance(transform.position, anchor.transform.position);

            // Cheap tests first - only pay for the cast on anchors that already qualify.
            if (distance > anchor.TetherRange || distance >= minDist)
                continue;

            if (!HasLineOfSight(anchor))
                continue;

            minDist = distance;
            closest = anchor;
        }
        return closest;
    }

    // True if nothing on losBlockingMask sits between the player and the anchor's attach point.
    private bool HasLineOfSight(AnchorBase anchor)
    {
        if (anchor == null)
            return false;

        Transform anchorPoint = GetAnchorPointTransform(anchor);
        if (anchorPoint == null)
            return false;

        Vector3 origin = transform.position + losOriginOffset;
        Vector3 delta = anchorPoint.position - origin;
        float dist = delta.magnitude;

        // Standing effectively on top of the anchor - nothing meaningful to test.
        if (dist <= losEndPadding)
            return true;

        Vector3 dir = delta / dist;
        float castDist = dist - losEndPadding;

        bool blocked = useThickCheck
            ? Physics.SphereCast(origin, losThickness, dir, out RaycastHit hit, castDist,
                                 losBlockingMask, QueryTriggerInteraction.Ignore)
            : Physics.Raycast(origin, dir, out hit, castDist,
                              losBlockingMask, QueryTriggerInteraction.Ignore);

        if (logLineOfSight)
        {
            if (blocked)
            {
                // A SphereCast that starts already overlapping geometry returns distance 0
                // and a zero normal. That's the classic false-block: the sweep never left
                // the origin, so the "blocker" is something the player is standing in/against.
                string degenerate = (hit.distance <= Mathf.Epsilon) ? " [DEGENERATE - sphere overlapped at origin]" : "";

                Debug.Log($"<color=red>[LOS]</color> {anchor.name} BLOCKED by '{hit.collider.name}' " +
                          $"(layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}) " +
                          $"at {hit.distance:F2}m of {castDist:F2}m{degenerate}", hit.collider);
            }
            else
            {
                Debug.Log($"<color=green>[LOS]</color> {anchor.name} clear ({castDist:F2}m)");
            }
        }

        return !blocked;
    }

    /// <summary>
    /// Start tethering if there is an anchor in range and in line of sight
    /// </summary>
    public void StartTether()
    {
        // Check for overcharge preventing tethering 
        if (playerOvercharge != null && !playerOvercharge.CanTether())
        {
            Debug.Log("Cannot tether: Overcharge in cooldown");
            return;
        }

        // currentAnchor is already LOS-filtered by GetClosestAnchorInRange().
        if (currentAnchor == null)
            return;

        // AnchorTether must receive the Transform of the AnchorPoint child of the current anchor (or the anchor itself if no child exists)
        Transform anchorBaseTransform = GetAnchorPointTransform(currentAnchor);

        // Sent Transform to AnchorTether
        if (anchorTether != null)
            anchorTether.SetEndPoint(anchorBaseTransform, true);

        // Activate the tether
        isTethered = true;
        attachedAnchor = currentAnchor;

        RuntimeManager.PlayOneShot(tetherAttachEvent, transform.position);
        attachedAnchor.Activate();
        OnTetherStarted?.Invoke(attachedAnchor);
        OnAnchorChanged?.Invoke(attachedAnchor);
    }

    /// <summary>
    /// Release tethering
    /// </summary>
    public void ReleaseTether()
    {
        isTethered = false;
        attachedAnchor = null;

        if (anchorTether != null)
            anchorTether.SetEndPoint(null, true);
        OnTetherReleased?.Invoke();
        OnAnchorChanged?.Invoke(null);
    }

    private Transform GetAnchorPointTransform(AnchorBase anchor)
    {
        if (anchor == null)
            return null;

        Transform child = anchor.transform.Find("AnchorPoint");
        return child != null ? child : anchor.transform;
    }

    public void RefreshAnchors()
    {
        allAnchors = FindObjectsByType<AnchorBase>(FindObjectsSortMode.None);
        Debug.Log($"[PlayerAnchor] Refreshed — now aware of {allAnchors.Length} anchor(s).");
    }

#if UNITY_EDITOR
    // Visualizes the LOS check to the nearest candidate: green = clear, red = blocked.
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || allAnchors == null)
            return;

        Vector3 origin = transform.position + losOriginOffset;

        foreach (AnchorBase anchor in allAnchors)
        {
            if (anchor == null)
                continue;

            float distance = Vector3.Distance(transform.position, anchor.transform.position);
            if (distance > anchor.TetherRange)
                continue;

            Gizmos.color = HasLineOfSight(anchor) ? Color.green : Color.red;
            Gizmos.DrawLine(origin, GetAnchorPointTransform(anchor).position);
        }
    }
#endif
}