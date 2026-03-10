// PlayerGrapple
// Handles the player's grapple mechanic. Each frame it finds the closest GrappleTowerManager
// within range and tracks it as the current tower. Listens for the grapple input button to
// start or release the grapple, and automatically stops if the tower goes out of range.

using UnityEngine;

public class PlayerGrapple : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private string grappleButton = "Fire3";

    private GrappleTowerManager[] allTowers;
    private GrappleTowerManager currentTower;

    [SerializeField] private bool isGrappling;
    public bool IsGrappling => isGrappling;
    public GrappleTowerManager CurrentTower => currentTower;

    private void Awake()
    {
        allTowers = FindObjectsByType<GrappleTowerManager>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        UpdateCurrentTower();
        HandleInput();
        ValidateGrapple();
    }

    private void UpdateCurrentTower()
    {
        currentTower = GetClosestTowerInRange();
    }

    private void HandleInput()
    {
        if (Input.GetButtonDown(grappleButton))
        {
            if (isGrappling) // toggles grapple tether
                ReleaseGrapple();
            else
                StartGrapple();
        }
    }

    private void ValidateGrapple()
    {
        if (isGrappling && currentTower == null)
        {
            ReleaseGrapple();
        }
    }

    private GrappleTowerManager GetClosestTowerInRange()
    {
        GrappleTowerManager closest = null;
        float minDist = float.MaxValue;

        foreach (GrappleTowerManager tower in allTowers)
        {
            float distance = Vector3.Distance(transform.position, tower.transform.position);

            if (distance <= tower.GrappleRange && distance < minDist)
            {
                minDist = distance;
                closest = tower;
            }
        }

        return closest;
    }

    /// <summary>
    /// Start grappling if there is a tower in range
    /// </summary>
    public void StartGrapple()
    {
        if (currentTower != null)
            isGrappling = true;
    }

    /// <summary>
    /// Release grappling
    /// </summary>
    public void ReleaseGrapple()
    {
        isGrappling = false;
    }
}

