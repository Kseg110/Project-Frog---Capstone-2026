using UnityEngine;

public class PlayerAnchor : MonoBehaviour
{
    private AnchorBase[] allAnchors;
    private AnchorBase currentAnchor;
    private bool isTethered;

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
        if (Input.GetButtonDown("Fire3"))
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
        if (currentAnchor != null)
            isTethered = true;
    }

    /// <summary>
    /// Release tethering
    /// </summary>
    public void ReleaseTether()
    {
        isTethered = false;
    }
}