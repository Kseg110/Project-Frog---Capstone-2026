using Assets.Scripts.Player;
using UnityEngine;

public class PlayerTongueAttack : MonoBehaviour
{
    [SerializeField] private Transform tongueMesh;
    [SerializeField] private float maxLength = 10f;
    [SerializeField] private float extendSpeed = 20f;
    [SerializeField] private float retractSpeed = 25f;
    [SerializeField] private float tongueWidth = 0.3f;

    [Header("Wall Collision")]
    [Tooltip("Layers that block the tongue and cause it to reel back on contact (e.g. Walls).")]
    [SerializeField] private LayerMask wallLayers;

    [Tooltip("Optional origin the wall-check ray fires from. If null, uses this object's transform. Set to a muzzle/fire point for best accuracy since the tongue mesh pivot moves during animation.")]
    [SerializeField] private Transform rayOrigin;

    [Tooltip("Small buffer so the tongue stops just short of the wall instead of clipping into it.")]
    [SerializeField] private float wallSkin = 0.1f;

    private float currentLength = 0f;
    private bool extending = false;
    private bool retracting = false;
    private Vector3 tongueLocation;
    public bool IsActive => extending || retracting;
    public System.Action OnTongueFinished;

    private PlayerAnimation playerAnimation;

    private void Awake()
    {
        if (tongueMesh == null)
        {
            Debug.LogError($"Please assign tongueMesh in ${gameObject.name}", this);
        }
        tongueLocation = tongueMesh.localPosition;

        playerAnimation = GetComponentInChildren<PlayerAnimation>();
    }

    /// <summary>
    /// Begins extending the tongue. Does nothing if already retracting. + Animation
    /// </summary>
    
    public void TongueAnimHelper()
    {
        playerAnimation.PlayTongueAttack();
        BeginTongueExtend();
    }
    public void BeginTongueExtend()
    {
        if (retracting) return;
        extending = true;
        retracting = false;
    }

    /// <summary>
    /// Begins retracting the tongue. If already retracting, does nothing.
    /// Call this when letting go of Fire2, or hitting an enemy/fly to instantly begin retracting.
    /// </summary>
    public void BeginTongueRetract()
    {
        if (!retracting)
        {
            extending = false;
            retracting = true;
        }
    }

    private void Update()
    {
        if (extending)
        {
            currentLength += extendSpeed * Time.deltaTime;

            // Reel back if the tongue would pass through a wall this frame.
            // Clamp the visual flush to the wall, then flip to retracting.
            if (CheckWallHit(currentLength, out float clampedLength))
            {
                currentLength = clampedLength;
                extending = false;
                BeginTongueRetract();
            }
            else if (currentLength >= maxLength)
            {
                currentLength = maxLength;
                extending = false;
                BeginTongueRetract();
            }
        }
        else if (retracting)
        {
            currentLength -= retractSpeed * Time.deltaTime;
            if (currentLength <= 0f)
            {
                currentLength = 0f;
                retracting = false;
                OnTongueFinished?.Invoke();
            }
        }

        UpdateTongueVisual();
    }

    // Raycasts along the tongue's forward from the origin out to the current length.
    private bool CheckWallHit(float length, out float clampedLength)
    {
        clampedLength = length;

        Transform origin = rayOrigin != null ? rayOrigin : transform;

        if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, length, wallLayers, QueryTriggerInteraction.Ignore))
        {
            clampedLength = Mathf.Max(0f, hit.distance - wallSkin);
            return true;
        }

        return false;
    }

    private void UpdateTongueVisual()
    {
        tongueMesh.localScale = new Vector3(tongueWidth, currentLength / 2f, tongueWidth);
        tongueMesh.localPosition = tongueLocation + new Vector3(0, 0, currentLength / 2f); //MAKE THIS 0, 0, 0 WHEN FINAL MESH IS ADDED, And make sure the pivot for that mesh isn't dead center.
    }
}