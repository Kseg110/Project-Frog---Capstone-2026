// PlayerAnchor
// Handles the player's anchor mechanic. Each frame it finds the closest Anchor
// within range and tracks it as the current tower. Listens for the grapple input button to
// start or release the grapple, and automatically stops if the tower goes out of range.

using UnityEngine;

public class PlayerAnchor : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private string TetherButton = "Fire3";

    private AnchorBase[] allAnchors;
    private AnchorBase currentAnchor;

    [SerializeField] private bool isTethered;
    public bool IsTethered => isTethered;
    public AnchorBase CurrentAnchor => currentAnchor;

    private void Awake()
    {
        allAnchors = FindObjectsByType<AnchorBase>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        UpdateCurrentAnchor();
        HandleInput();
        ValidateTether();
    }

    private void UpdateCurrentAnchor()
    {
        currentAnchor = GetClosestAnchorInRange();
    }

    private void HandleInput()
    {
        if (Input.GetButtonDown(TetherButton))
        {
            if (isTethered) // toggles tether
                ReleaseTether();
            else
                StartTether();
        }
    }

    private void ValidateTether()
    {
        if (isTethered && currentAnchor == null)
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
    /// Start tethering if there is a anchor in range
    /// </summary>
    public void StartTether()
    {
        if (currentAnchor != null)
            isTethered = true;
    }

    /// <summary>
    /// Release Tethering
    /// </summary>
    public void ReleaseTether()
    {
        isTethered = false;
    }
}

