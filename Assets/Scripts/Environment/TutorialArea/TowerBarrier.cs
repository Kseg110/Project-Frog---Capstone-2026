using UnityEngine;

/// <summary>
/// Invisible barrier that only allows the player to pass when they are tethered to a tower.
/// Usage:
/// - Attach this to a GameObject that has a trigger Collider (isTrigger=true) to detect the player.
/// - The same GameObject (or a child) should contain a non-trigger Collider that physically blocks the player.
/// - The script will toggle collision between the blocking collider and the player's collider while the player
///   is inside the trigger based on PlayerAnchor.IsTethered.
/// </summary>
public class TowerBarrier : MonoBehaviour
{
    [Tooltip("Player tag used to identify the player collider")]
    public string playerTag = "Player";

    [Tooltip("Optional explicit blocking collider. If null the script will find a non-trigger collider on this object or children.")]
    public Collider blockingCollider;

    // runtime
    private Collider playerCollider;
    private PlayerAnchor playerAnchor;
    private bool playerInside = false;
    private PlayerMovement playerMovement;
    private Collider[] playerColliders = null;
    private float lastTetheredTime = -Mathf.Infinity;
    [Tooltip("Grace period after tether release during which a dash still counts as 'tethered' (seconds)")]
    public float tetherGracePeriod = 0.2f;
    private bool currentIgnore = false;
    [Tooltip("When conditions are met, disable the blocking collider for this many seconds to allow passage.")]
    public float temporaryDisableDuration = 1.0f;
    private bool disableInProgress = false;
    // Debug toggles visible in inspector to help diagnose behavior at runtime
    [Header("Debug (runtime)")]
    public bool debugIsTethered = false;
    public bool debugIsDashing = false;
    public bool debugWasRecentlyTethered = false;
    public bool debugAllowPass = false;
    // debug
    private bool prevIsDashing = false;
    private bool prevTethered = false;
    private bool prevShouldIgnore = false;
    private float debugLogTimer = 0f;

    private void Start()
    {
        if (blockingCollider == null)
        {
            // find a non-trigger collider on self or children
            var cols = GetComponentsInChildren<Collider>(true);
            foreach (var c in cols)
            {
                if (c != null && !c.isTrigger)
                {
                    blockingCollider = c;
                    break;
                }
            }
        }

        if (blockingCollider == null)
            Debug.LogWarning($"[TutorialArea] No blocking (non-trigger) collider found on '{gameObject.name}'. This barrier will not block.");
        else
        {
            // warn if blocking collider is also a trigger collider on this object (conflict)
            var triggerCols = GetComponentsInChildren<Collider>(true);
            foreach (var tc in triggerCols)
            {
                if (tc == null) continue;
                if (tc == blockingCollider && tc.isTrigger)
                {
                    Debug.LogWarning($"TowerBarrier: blockingCollider is also a trigger on '{gameObject.name}'. The trigger and blocking collider must be different colliders.");
                    break;
                }
            }
        }
    }

    private void Update()
    {
        // while the player is inside the detection trigger, poll tether state and update collision
        if (!playerInside || playerCollider == null || blockingCollider == null) return;

        if (playerAnchor == null)
            playerAnchor = playerCollider.GetComponentInParent<PlayerAnchor>();

        if (playerMovement == null)
            playerMovement = playerCollider.GetComponentInParent<PlayerMovement>();

        // update last tethered time while tethered
        if (playerAnchor != null && playerAnchor.IsTethered)
            lastTetheredTime = Time.time;

        bool isDashing = playerMovement != null && playerMovement.IsDashing;
        bool wasRecentlyTethered = (Time.time - lastTetheredTime) <= tetherGracePeriod;

        // allow pass-through only if the player is dashing and was tethered recently (or still tethered)
        bool tetheredNow = playerAnchor != null && playerAnchor.IsTethered;
        bool shouldIgnore = isDashing && (tetheredNow || wasRecentlyTethered);

        // expose for inspector debugging
        debugIsDashing = isDashing;
        debugIsTethered = tetheredNow;
        debugWasRecentlyTethered = wasRecentlyTethered;
        debugAllowPass = shouldIgnore;

        // throttle debug logs to once per 0.2s unless values change
        debugLogTimer -= Time.deltaTime;
        if (debugLogTimer <= 0f || isDashing != prevIsDashing || tetheredNow != prevTethered || shouldIgnore != prevShouldIgnore)
        {
            debugLogTimer = 0.2f;
            Debug.Log($"TowerBarrier: inside={playerInside} isDashing={isDashing} tetheredNow={tetheredNow} wasRecentlyTethered={wasRecentlyTethered} shouldIgnore={shouldIgnore} currentIgnore={currentIgnore} lastTetheredTime={lastTetheredTime} time={Time.time}");
            if (playerColliders != null)
            {
                string names = "";
                foreach (var pc in playerColliders) if (pc != null) names += pc.gameObject.name + ",";
                Debug.Log($"TowerBarrier: playerColliders ({playerColliders.Length}): {names}");
            }
        }

        if (shouldIgnore != currentIgnore)
        {
            // Instead of per-collider IgnoreCollision, briefly disable the blocking collider to allow passage.
            if (shouldIgnore)
            {
                TryTemporarilyDisableBlocking();
            }
            currentIgnore = shouldIgnore;
        }

        // Manual inspector override for quick debugging: if the debug checkbox is enabled and player is inside,
        // force a temporary disable so you can test passing through in the editor.
        if (debugAllowPass && playerInside && !disableInProgress)
        {
            Debug.Log("TowerBarrier: debugAllowPass triggered - disabling blocking collider for debug.");
            TryTemporarilyDisableBlocking();
        }

        prevIsDashing = isDashing;
        prevTethered = tetheredNow;
        prevShouldIgnore = shouldIgnore;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Accept the trigger if the collider belongs to the player (by PlayerMovement or PlayerAnchor) or matches tag
        var pm = other.GetComponentInParent<PlayerMovement>();
        var pa = other.GetComponentInParent<PlayerAnchor>();
        if (pm == null && pa == null && !other.CompareTag(playerTag))
            return;

        playerInside = true;
        playerCollider = other;
        playerAnchor = pa != null ? pa : other.GetComponentInParent<PlayerAnchor>();
        playerMovement = pm != null ? pm : other.GetComponentInParent<PlayerMovement>();

        // gather all colliders that belong to the player's root GameObject so we can ignore collisions robustly
        if (playerMovement != null)
        {
            playerColliders = playerMovement.GetComponentsInChildren<Collider>(true);
        }
        else
        {
            // fallback: only the entering collider
            playerColliders = new Collider[] { playerCollider };
        }

        // initialize lastTetheredTime
        if (playerAnchor != null && playerAnchor.IsTethered)
            lastTetheredTime = Time.time;

        // initialize collision state
        currentIgnore = false;
        bool isDashing = playerMovement != null && playerMovement.IsDashing;
        bool wasRecentlyTethered = (Time.time - lastTetheredTime) <= tetherGracePeriod;
        bool shouldIgnore = isDashing && (playerAnchor != null && (playerAnchor.IsTethered || wasRecentlyTethered));
        if (blockingCollider != null && playerColliders != null)
        {
            foreach (var pc in playerColliders)
            {
                if (pc == null) continue;
                Physics.IgnoreCollision(blockingCollider, pc, shouldIgnore);
            }
        }
        currentIgnore = shouldIgnore;
    }

    private void OnTriggerExit(Collider other)
    {
        // Only clear state when the collider leaving belongs to the same player we tracked
        var pm = other.GetComponentInParent<PlayerMovement>();
        if (playerMovement != null)
        {
            if (pm != playerMovement)
                return;
        }
        else
        {
            // fallback: if no PlayerMovement tracked, ensure this is the same collider that entered
            if (other != playerCollider && !other.CompareTag(playerTag))
                return;
        }

        if (blockingCollider != null && playerColliders != null)
        {
            // restore collision when player leaves
            foreach (var pc in playerColliders)
            {
                if (pc == null) continue;
                Physics.IgnoreCollision(blockingCollider, pc, false);
            }
        }

        playerInside = false;
        playerCollider = null;
        playerAnchor = null;
        playerMovement = null;
        playerColliders = null;
    }

    private void TryTemporarilyDisableBlocking()
    {
        if (blockingCollider == null) return;
        if (disableInProgress) return;

        StartCoroutine(TemporaryDisableCoroutine());
    }

    private System.Collections.IEnumerator TemporaryDisableCoroutine()
    {
        disableInProgress = true;
        bool previousEnabled = blockingCollider.enabled;
        blockingCollider.enabled = false;
        Debug.Log($"TowerBarrier: Temporarily disabled blocking collider '{blockingCollider.name}' for {temporaryDisableDuration} seconds.");
        yield return new WaitForSeconds(temporaryDisableDuration);
        if (blockingCollider != null)
        {
            blockingCollider.enabled = previousEnabled;
            Debug.Log($"TowerBarrier: Re-enabled blocking collider '{blockingCollider.name}'.");
        }
        disableInProgress = false;
    }
}
