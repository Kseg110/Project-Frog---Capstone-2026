using System.Collections.Generic;
using UnityEngine;

public class CameraPanEffect : CameraEffectBase
{

    [Header("Pan Settings")]
    [SerializeField] private float panTime = 1.0f;
    [SerializeField] private bool usePlayerOffset = true;
    [SerializeField] private DoorSystem doorSystem;
    [Header("Player Control")]
    [SerializeField] private bool pausePlayerDuringPan = true;

    private enum State { Idle, PanningToPOI, Holding, Returning }
    private State state = State.Idle;


    private int currentPanIndex = 0;
    private float currentHoldTime = 0f;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private List<CameraPanRoundTrigger.PanPoint> panPoints = new List<CameraPanRoundTrigger.PanPoint>();
    private float timer;
    private float holdTimer;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 offsetFromPlayer;
    private Transform playerTransform;
    private int doorIndexToReady = -1;
    private CameraController controller;
    private PlayerMovement playerMovement;
    private bool playerPaused;
    private bool doorReadyTriggered = false;

    private void Awake()
    {
        controller = GetComponentInParent<CameraController>();
        // cache player transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        playerMovement = playerTransform != null ? playerTransform.GetComponent<PlayerMovement>() : null;
        if (doorSystem == null)
            doorSystem = FindAnyObjectByType<DoorSystem>();
    }

    private void OnEnable()
    {
        if (controller == null)
            controller = GetComponentInParent<CameraController>();

        controller?.AddEffect(this);
    }

    private void OnDisable()
    {
        controller?.RemoveEffect(this);
        if (playerPaused)
            ResumePlayer();
    }

    public override Vector3 ApplyEffect(float deltaTime)
    {
        if (state == State.Idle || panPoints.Count == 0)
            return Vector3.zero;

        Vector3 desiredPosition = originalPosition;
        Quaternion desiredRotation = originalRotation;


        if (state == State.PanningToPOI)
        {
            timer += deltaTime;

            float t = Mathf.Clamp01(timer / panTime);

            desiredPosition = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            desiredRotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );


            // reached point of interest
            if (timer >= panTime)
            {
                timer = 0f;
                holdTimer = 0f;

                // snap exactly to point
                desiredPosition = targetPosition;
                desiredRotation = targetRotation;

                // Mark door ready when reaching last point (before holding)
                if (currentPanIndex == panPoints.Count - 1 && !doorReadyTriggered)
                {
                    MarkDoorReady();
                    doorReadyTriggered = true;
                }

                // ONLY stop if this point has hold time
                if (currentHoldTime > 0f)
                {
                    state = State.Holding;
                }
                else
                {
                    // No hold = immediately continue
                    if (currentPanIndex < panPoints.Count - 1)
                    {
                        currentPanIndex++;

                        startPosition = targetPosition;
                        startRotation = targetRotation;

                        SetNextPanPoint();

                        state = State.PanningToPOI;
                    }
                    else
                    {
                        // Last point, start return
                        startPosition = targetPosition;
                        startRotation = targetRotation;

                        state = State.Returning;
                    }
                }
            }
        }


        else if (state == State.Holding)
        {
            desiredPosition = targetPosition;
            desiredRotation = targetRotation;

            holdTimer += deltaTime;

            if (currentHoldTime <= 0f || holdTimer >= currentHoldTime)
            {
                holdTimer = 0f;
                timer = 0f;

                if (currentPanIndex == panPoints.Count - 1)
                {
                    startPosition = targetPosition;
                    startRotation = targetRotation;

                    state = State.Returning;
                }
                else
                {
                    currentPanIndex++;

                    startPosition = targetPosition;
                    startRotation = targetRotation;

                    SetNextPanPoint();

                    state = State.PanningToPOI;
                }
            }
        }

        else if (state == State.Returning)
        {
            timer += deltaTime;

            float t = Mathf.Clamp01(timer / panTime);
            t = EaseInOutQuad(t);

            desiredPosition = Vector3.Lerp(
                startPosition,
                originalPosition,
                t
            );

            desiredRotation = Quaternion.Slerp(
                startRotation,
                originalRotation,
                t
            );

            if (timer >= panTime)
            {
                timer = 0f;
                state = State.Idle;
            }
        }


        transform.rotation = desiredRotation;


        Vector3 effectOffset = controller != null
            ? desiredPosition - controller.GetBasePosition()
            : desiredPosition - transform.position;


        if (state == State.Idle && playerPaused)
            ResumePlayer();


        return effectOffset;
    }

    public void TriggerPan(
        List<CameraPanRoundTrigger.PanPoint> points,
        float time,
        int doorIndex)
    {
        if (points == null || points.Count == 0)
        {
            Debug.LogWarning("CameraPanEffect: No pan points.");
            return;
        }

        panPoints = points;
        currentPanIndex = 0;
        doorIndexToReady = doorIndex;
        doorReadyTriggered = false; // Reset flag
        panTime = time;


        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            playerMovement = playerTransform != null
                ? playerTransform.GetComponent<PlayerMovement>()
                : null;
        }

        if (controller == null)
            controller = GetComponentInParent<CameraController>();

        originalPosition = transform.position;
        originalRotation = transform.rotation;

        startPosition = originalPosition;
        startRotation = originalRotation;

        SetNextPanPoint();

        if (pausePlayerDuringPan && !playerPaused && playerMovement != null)
        {
            playerMovement.StopMovement();
            playerPaused = true;
        }


        timer = 0f;
        holdTimer = 0f;

        state = State.PanningToPOI;
    }

    private void SetNextPanPoint()
    {
        if (currentPanIndex < 0 || currentPanIndex >= panPoints.Count)
            return;


        CameraPanRoundTrigger.PanPoint point = panPoints[currentPanIndex];


        // Each point has its own hold time
        currentHoldTime = point.holdTime;


        Vector3 current = point.pointOfInterest.position;

        if (currentPanIndex < panPoints.Count - 1)
        {
            Vector3 next = panPoints[currentPanIndex + 1].pointOfInterest.position;

            // Pull the target toward the next point for smoother turns.
            current = Vector3.Lerp(current, next, 0.15f);
        }

        if (usePlayerOffset && playerTransform != null)
        {
            offsetFromPlayer = originalPosition - playerTransform.position;
            targetPosition = current + offsetFromPlayer;
        }
        else
        {
            targetPosition = current;
        }


        Vector3 lookDir = point.pointOfInterest.position - targetPosition;

        targetRotation = lookDir.sqrMagnitude <= 0.0001f
            ? originalRotation
            : Quaternion.LookRotation(lookDir.normalized, Vector3.up);
    }

    private void MarkDoorReady()
    {
        if (doorSystem != null && doorIndexToReady >= 0)
        {
            doorSystem.SetDoorReady(doorIndexToReady);
            Debug.Log($"CameraPanEffect: Door index {doorIndexToReady} marked as ready.");
            doorIndexToReady = -1;
        }
    }

    private void ResumePlayer()
    {
        if (!playerPaused)
            return;

        if (playerMovement == null && playerTransform != null)
            playerMovement = playerTransform.GetComponent<PlayerMovement>();

        playerMovement?.ResumeMovement();
        playerPaused = false;
    }

    private static float EaseInOutQuad(float t)
    {
        if (t < 0.5f) return 2f * t * t;
        return -1f + (4f - 2f * t) * t;
    }

    public bool IsPanning => state != State.Idle;
}