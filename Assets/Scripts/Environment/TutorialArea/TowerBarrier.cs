using UnityEngine;

/// <summary>
/// Tower barrier controlled by the player's tether and dash state.
///
/// Behavior:
///
/// 1. Player does NOT need to be inside the trigger to activate the barrier.
/// 2. Player must be tethered AND dashing at the same time.
/// 3. When tethered + dashing starts, the barrier disables.
/// 4. Barrier can stay disabled for up to temporaryDisableDuration seconds.
/// 5. If the player enters the barrier trigger while it is disabled,
///    the barrier immediately re-enables.
/// 6. If the player stops dashing, the barrier re-enables.
/// 7. If tether is lost, a short tether grace period is used before
///    closing the barrier.
/// 8. The barrier can activate again on another tethered dash.
/// </summary>
public class TowerBarrier : MonoBehaviour
{
    [Header("Player References")]

    [Tooltip("Player's PlayerAnchor component.")]
    public PlayerAnchor playerAnchor;

    [Tooltip("Player's PlayerMovement component.")]
    public PlayerMovement playerMovement;


    [Header("Barrier")]

    [Tooltip("The physical collider that blocks the player.")]
    public Collider blockingCollider;

    [Tooltip("Maximum time the barrier remains disabled.")]
    public float temporaryDisableDuration = 10f;


    [Header("Tether Settings")]

    [Tooltip(
        "How long after IsTethered becomes false before the barrier " +
        "treats the player as completely untethered."
    )]
    public float tetherReleaseDelay = 1f;


    [Header("Trigger Settings")]

    [Tooltip(
        "When enabled, entering this object's trigger while the barrier " +
        "is disabled immediately closes the barrier."
    )]
    public bool reactivateOnTriggerEnter = true;


    [Header("Debug")]

    public bool debugIsTethered;
    public bool debugIsDashing;
    public bool debugTetherGraceActive;
    public bool debugAllowPass;
    public bool debugBarrierOpen;
    public float debugRemainingTime;
    public float debugTetherReleaseTimer;


    // ---------------------------------------------------------
    // INTERNAL STATE
    // ---------------------------------------------------------

    private bool dashSequenceStarted = false;

    private bool previousTethered = false;

    private float tetherReleaseTimer = 0f;

    private Coroutine barrierCoroutine;

    private void FindPlayerReferences()
    {
        // If already assigned, keep the assigned references.
        if (playerAnchor != null && playerMovement != null)
            return;

        // Find PlayerAnchor anywhere in the currently loaded scenes.
        if (playerAnchor == null)
        {
            playerAnchor = FindFirstObjectByType<PlayerAnchor>();
        }

        // Find PlayerMovement anywhere in the currently loaded scenes.
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (playerAnchor != null)
        {
            Debug.Log(
                $"TowerBarrier: Found PlayerAnchor '{playerAnchor.name}' " +
                $"in scene '{playerAnchor.gameObject.scene.name}'."
            );
        }

        if (playerMovement != null)
        {
            Debug.Log(
                $"TowerBarrier: Found PlayerMovement '{playerMovement.name}' " +
                $"in scene '{playerMovement.gameObject.scene.name}'."
            );
        }
    }
    // ---------------------------------------------------------
    // START
    // ---------------------------------------------------------

    private void Start()
    {
        FindBlockingCollider();
        FindPlayerReferences();

        if (blockingCollider == null)
        {
            Debug.LogWarning(
                $"TowerBarrier: No non-trigger blocking collider found on '{gameObject.name}'."
            );
        }


        if (playerAnchor == null)
        {
            Debug.LogWarning(
                $"TowerBarrier: PlayerAnchor is not assigned on '{gameObject.name}'."
            );
        }


        if (playerMovement == null)
        {
            Debug.LogWarning(
                $"TowerBarrier: PlayerMovement is not assigned on '{gameObject.name}'."
            );
        }


        // Barrier starts active.
        CloseBarrier();


        if (playerAnchor != null)
        {
            previousTethered =
                playerAnchor.IsTethered;
        }
    }


    // ---------------------------------------------------------
    // UPDATE
    // ---------------------------------------------------------

    private void Update()
    {
        if (playerAnchor == null ||
            playerMovement == null ||
            blockingCollider == null)
        {
            return;
        }


        bool rawTethered =
            playerAnchor.IsTethered;

        bool isDashing =
            playerMovement.IsDashing;


        // -----------------------------------------------------
        // TETHER RELEASE DELAY
        // -----------------------------------------------------

        if (rawTethered)
        {
            // Player is tethered.
            tetherReleaseTimer = 0f;
        }
        else
        {
            // Player is no longer tethered.
            if (previousTethered)
            {
                tetherReleaseTimer += Time.deltaTime;
            }
            else if (tetherReleaseTimer > 0f)
            {
                tetherReleaseTimer += Time.deltaTime;
            }
        }


        // The player is considered tethered during the release delay.
        bool effectiveTethered =
            rawTethered ||
            tetherReleaseTimer < tetherReleaseDelay;


        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        debugIsTethered = rawTethered;
        debugIsDashing = isDashing;

        debugTetherReleaseTimer =
            tetherReleaseTimer;

        debugTetherGraceActive =
            !rawTethered &&
            tetherReleaseTimer < tetherReleaseDelay;


        // -----------------------------------------------------
        // VALID PASS CONDITION
        // -----------------------------------------------------

        bool allowPass =
            effectiveTethered &&
            isDashing;

        debugAllowPass = allowPass;


        // -----------------------------------------------------
        // START BARRIER
        // -----------------------------------------------------

        if (allowPass)
        {
            if (!dashSequenceStarted)
            {
                dashSequenceStarted = true;

                Debug.Log(
                    "TowerBarrier: TETHERED + DASHING detected. " +
                    "Disabling barrier."
                );

                StartBarrier();
            }
        }


        // -----------------------------------------------------
        // STOP DASH
        // -----------------------------------------------------

        if (!isDashing)
        {
            if (dashSequenceStarted)
            {
                dashSequenceStarted = false;

                CloseBarrier();

                Debug.Log(
                    "TowerBarrier: Dash ended. Barrier reactivated."
                );
            }
        }


        // -----------------------------------------------------
        // COMPLETELY UNTETHERED
        // -----------------------------------------------------

        if (!effectiveTethered)
        {
            if (dashSequenceStarted)
            {
                dashSequenceStarted = false;

                CloseBarrier();

                Debug.Log(
                    "TowerBarrier: Tether release delay expired. " +
                    "Barrier reactivated."
                );
            }
        }


        previousTethered =
            rawTethered;
    }


    // ---------------------------------------------------------
    // START BARRIER TIMER
    // ---------------------------------------------------------

    private void StartBarrier()
    {
        if (blockingCollider == null)
            return;


        // Stop an old timer if one exists.
        if (barrierCoroutine != null)
        {
            StopCoroutine(barrierCoroutine);
            barrierCoroutine = null;
        }


        barrierCoroutine =
            StartCoroutine(
                BarrierOpenCoroutine()
            );
    }


    // ---------------------------------------------------------
    // BARRIER COROUTINE
    // ---------------------------------------------------------

    private System.Collections.IEnumerator BarrierOpenCoroutine()
    {
        if (blockingCollider == null)
            yield break;


        // -----------------------------------------------------
        // DISABLE BARRIER
        // -----------------------------------------------------

        blockingCollider.enabled = false;

        debugBarrierOpen = true;


        Debug.Log(
            $"TowerBarrier: Barrier DISABLED for up to " +
            $"{temporaryDisableDuration} seconds."
        );


        float timer = 0f;


        // -----------------------------------------------------
        // TIMER
        // -----------------------------------------------------

        while (timer < temporaryDisableDuration)
        {
            // Safety check.
            if (playerAnchor == null ||
                playerMovement == null)
            {
                break;
            }


            bool isTethered =
                playerAnchor.IsTethered;

            bool isDashing =
                playerMovement.IsDashing;


            // -------------------------------------------------
            // STOP IF DASH ENDS
            // -------------------------------------------------

            if (!isDashing)
            {
                Debug.Log(
                    "TowerBarrier: Player stopped dashing. " +
                    "Reactivating barrier."
                );

                break;
            }


            // -------------------------------------------------
            // STOP IF COMPLETELY UNTETHERED
            // -------------------------------------------------

            bool stillEffectivelyTethered =
                isTethered ||
                tetherReleaseTimer < tetherReleaseDelay;


            if (!stillEffectivelyTethered)
            {
                Debug.Log(
                    "TowerBarrier: Player is no longer tethered. " +
                    "Reactivating barrier."
                );

                break;
            }


            // Increase timer.
            timer += Time.deltaTime;


            debugRemainingTime =
                Mathf.Max(
                    0f,
                    temporaryDisableDuration - timer
                );


            yield return null;
        }


        // -----------------------------------------------------
        // TIMER EXPIRED OR CONDITION ENDED
        // -----------------------------------------------------

        CloseBarrier();

        dashSequenceStarted = false;
        barrierCoroutine = null;


        Debug.Log(
            "TowerBarrier: Barrier timer finished. " +
            "Barrier REACTIVATED."
        );
    }


    // ---------------------------------------------------------
    // TRIGGER
    // ---------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        if (!reactivateOnTriggerEnter)
            return;


        if (!debugBarrierOpen)
            return;


        // Check whether this is the player.
        PlayerMovement pm =
            other.GetComponentInParent<PlayerMovement>();

        PlayerAnchor pa =
            other.GetComponentInParent<PlayerAnchor>();


        if (pm == null && pa == null)
            return;


        // -----------------------------------------------------
        // PLAYER PASSED THROUGH THE BARRIER
        // -----------------------------------------------------

        Debug.Log(
            "TowerBarrier: Player passed through trigger. " +
            "Reactivating barrier immediately."
        );


        // Stop timer.
        if (barrierCoroutine != null)
        {
            StopCoroutine(barrierCoroutine);
            barrierCoroutine = null;
        }


        // Immediately close.
        CloseBarrier();


        dashSequenceStarted = false;
    }


    // ---------------------------------------------------------
    // CLOSE BARRIER
    // ---------------------------------------------------------

    private void CloseBarrier()
    {
        if (blockingCollider != null)
        {
            blockingCollider.enabled = true;
        }


        debugBarrierOpen = false;
        debugRemainingTime = 0f;
    }


    // ---------------------------------------------------------
    // FIND BLOCKING COLLIDER
    // ---------------------------------------------------------

    private void FindBlockingCollider()
    {
        if (blockingCollider != null)
            return;


        Collider[] colliders =
            GetComponentsInChildren<Collider>(true);


        foreach (Collider col in colliders)
        {
            if (col == null)
                continue;


            // We want the physical collider,
            // not the trigger.
            if (!col.isTrigger)
            {
                blockingCollider = col;
                break;
            }
        }
    }
}