using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerAnchor : MonoBehaviour
{
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

        // AnchorTether must receive the Transform of the AnchorPoint child of the current anchor (or the anchor itself if no child exists)
        Transform anchorBaseTransform = currentAnchor.transform;

        // Sent Transform to AnchorTether
        if (anchorTether != null)
            anchorTether.SetEndPoint(anchorBaseTransform, true);

        // Activate the tether
        isTethered = true;
        OnTetherStarted?.Invoke(currentAnchor);
        OnAnchorChanged?.Invoke(currentAnchor);
    }

    /// <summary>
    /// Release tethering
    /// </summary>
    public void ReleaseTether()
    {
        isTethered = false;

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
}