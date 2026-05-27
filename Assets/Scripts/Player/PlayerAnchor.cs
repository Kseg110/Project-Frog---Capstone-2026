using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnchor : MonoBehaviour
{
    private AnchorBase[] allAnchors;
    private AnchorBase currentAnchor;
    private bool isTethered;

    private InputAction tetherAction;
    private PlayerInput playerInput;
    private string currentActionMapName;
    [SerializeField] private AnchorTether anchorTether;

    public bool IsTethered => isTethered;
    public AnchorBase CurrentAnchor => currentAnchor;

    private void Awake()
    {
        allAnchors = FindObjectsByType<AnchorBase>(FindObjectsSortMode.None);

        playerInput = GetComponent<PlayerInput>();
        Debug.Assert(playerInput != null, $"[{gameObject.name}] missing PlayerInput!", this);

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
        if (tetherAction != null)
        {
            if (tetherAction.WasPressedThisFrame())
            {
                if (isTethered)
                    ReleaseTether();
                else
                    StartTether();
            }
        }
    }

    private void ValidateTether()
    {
        if (isTethered && currentAnchor == null)
            ReleaseTether();
    }

    private AnchorBase GetClosestAnchorInRange()
    {
        AnchorBase closest = null;
        float minDist = float.MaxValue;

        foreach (AnchorBase anchor in allAnchors)
        {
            float distance = Vector3.Distance(transform.position, anchor.transform.position);
            if (distance <= anchor.TetherRange && distance < minDist)
            {
                minDist = distance;
                closest = anchor;
            }
        }
        return closest;
    }

    /// <summary>
    /// Start tethering if there is an anchor in range
    /// </summary>
    public void StartTether()
    {
        if (currentAnchor == null)
            return;

        Transform anchorPoint = GetAnchorPointTransform(currentAnchor);

        if (anchorTether != null)
            anchorTether.SetEndPoint(anchorPoint, true);

        isTethered = true;
    }

    /// <summary>
    /// Release tethering
    /// </summary>
    public void ReleaseTether()
    {
        isTethered = false;

        if (anchorTether != null)
            anchorTether.SetEndPoint(null, true);
    }

    private Transform GetAnchorPointTransform(AnchorBase anchor)
    {
        if (anchor == null)
            return null;

        Transform child = anchor.transform.Find("AnchorPoint");
        return child != null ? child : anchor.transform;
    }
}