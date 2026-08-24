using FMODUnity;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class CameraPanEffect : CameraEffectBase
{
    // =========================================================
    // PAN SETTINGS
    // =========================================================

    [Header("Pan Settings")]

    [Tooltip("Movement speed along the spline and between points.")]
    [SerializeField]
    private float moveSpeed = 20f;

    public static bool GlobalPanActive = false;

    [SerializeField]
    private bool GlobalPanActiveInspector;

    private Transform CameraTransform
    {
        get
        {
            Camera cam = Camera.main;

            if (cam == null)
                return null;

            return cam.transform;
        }
    }

    // =========================================================
    // PAUSE POINT TIMER
    // =========================================================

    [SerializeField]
    private float pausePointMoveTimer = 0f;

    // =========================================================
    // CAMERA STARTUP
    // =========================================================

    [Header("Camera Startup")]

    [SerializeField]
    private float cameraStartupDelay = 1f;

    [SerializeField]
    private bool cameraReady;

    [SerializeField]
    private float cameraReadyTimer;

    // =========================================================
    // CAMERA REFERENCE
    // =========================================================

    [Header("Camera Reference")]

    [SerializeField]
    private CameraController controller;

    [SerializeField]
    private Transform playerTransform;

    // =========================================================
    // POINT TO POINT
    // =========================================================

    [Header("Point To Point")]

    [SerializeField]
    private Transform panStart_EndObjects;

    [SerializeField]
    private float pointHeightOffset = 20f;

    [SerializeField]
    private float rotationSpeed = 180f;

    // =========================================================
    // PAUSE POINT
    // =========================================================

    [Header("Pause Point")]

    [SerializeField]
    private Transform PAUSEPOINT;

    // =========================================================
    // AUTO FIND
    // =========================================================

    [Header("Auto Find Pan / Pause")]

    [SerializeField]
    private string panPointTag = "PanPoint";

    [SerializeField]
    private string pausePointTag = "PausePoint";

    private void FindMissingPanPoints()
    {
        if (panStart_EndObjects == null)
        {
            GameObject panObject =
                FindGameObjectByTagSafe(panPointTag);

            if (panObject != null)
            {
                panStart_EndObjects =
                    panObject.transform;
            }
        }

        if (PAUSEPOINT == null)
        {
            GameObject pauseObject =
                FindGameObjectByTagSafe(pausePointTag);

            if (pauseObject != null)
            {
                PAUSEPOINT =
                    pauseObject.transform;
            }
        }
    }

    private GameObject FindGameObjectByTagSafe(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return null;

        try
        {
            return GameObject.FindGameObjectWithTag(tagName);
        }
        catch (UnityException)
        {
            Debug.LogWarning(
                $"[{nameof(CameraPanEffect)}] Tag '{tagName}' does not exist.",
                this
            );

            return null;
        }
    }

    // =========================================================
    // PAUSE POINT MOVEMENT
    // =========================================================

    [Header("Pause Point Movement")]

    [SerializeField]
    private float pausePointMoveSpeed = 20f;

    [SerializeField]
    private float pausePointRotationSpeed = 180f;

    [SerializeField]
    private bool movingToPausePoint;

    [SerializeField]
    private bool returningFromPausePoint;

    [SerializeField]
    public bool atPausePoint;

    [SerializeField]
    private Vector3 pauseReturnPosition;

    [SerializeField]
    private Quaternion pauseReturnRotation;

    // =========================================================
    // SPLINE
    // =========================================================

    [Header("Spline")]

    [SerializeField]
    private CameraPanRoundTrigger roundTrigger;

    // =========================================================
    // SPLINE HOLD DATA
    // =========================================================

    [Header("Spline Hold Times Runtime")]

    [SerializeField]
    private List<float> splineHoldTimes =
        new List<float>();

    [SerializeField]
    private int currentSplinePointIndex = 0;

    [SerializeField]
    private float splinePointHoldTimer = 0f;

    [SerializeField]
    private bool holdingAtSplinePoint = false;

    // =========================================================
    // SPLINE RUNTIME
    // =========================================================

    [Header("Spline Runtime")]

    [SerializeField]
    private SplineContainer activeSpline;

    [SerializeField]
    private float splineDistance;

    [SerializeField]
    private float splineLength;

    [SerializeField]
    private float splineProgressDebug;

    // =========================================================
    // SPLINE START MOVEMENT
    // =========================================================

    [Header("Spline Start Movement Debug")]

    [SerializeField]
    private float moveToStartSpeed = 20f;

    [SerializeField]
    private float debugDistanceToTravel;

    [SerializeField]
    private float debugDistanceTravelled;

    // =========================================================
    // POINT RUNTIME
    // =========================================================

    [Header("Point Runtime")]

    [SerializeField]
    private List<Vector3> pointPath =
        new List<Vector3>();

    [SerializeField]
    private int currentPointIndex;

    [SerializeField]
    private bool pointInitialized;

    [SerializeField]
    private float pointHoldTimer;

    [SerializeField]
    private bool holdingAtPoint;

    // =========================================================
    // POINT CLASS
    // =========================================================

    [System.Serializable]
    public class PanPoint
    {
        public Vector3 pointPosition;

        [Tooltip("How long the camera waits at this point.")]
        public float holdTime = 5f;
    }

    [SerializeField]
    private List<PanPoint> panPoints =
        new List<PanPoint>();

    private List<CameraPanRoundTrigger.PanPoint>
        activePanPoints;

    // =========================================================
    // RETURN CAMERA
    // =========================================================

    [Header("Return Camera")]

    [SerializeField]
    private Vector3 returnPosition;

    [SerializeField]
    private Quaternion returnRotation;

    [SerializeField]
    public bool ThisPansReturnTime = true;

    [SerializeField]
    private float returnTime = 1f;

    [SerializeField]
    private float returnTimer = 0f;

    private Vector3 returnStartPosition;
    private Quaternion returnStartRotation;

    // =========================================================
    // DOOR
    // =========================================================

    [Header("Door")]

    [SerializeField]
    private DoorSystem doorSystem;

    [SerializeField]
    private int doorIndexToReady = -1;

    [SerializeField]
    private bool doorReadyTriggered;

    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player Control")]

    [SerializeField]
    private bool pausePlayerDuringPan = true;

    [SerializeField]
    private bool playerPaused;

    // =========================================================
    // DASH SKIP
    // =========================================================

    [Header("Dash Skip")]

    [SerializeField]
    private bool allowDashSkip = true;

    // =========================================================
    // CAMERA STATE
    // =========================================================

    [Header("Camera State")]

    [SerializeField]
    private State state = State.Idle;

    [SerializeField]
    private PanMode panMode = PanMode.PointToPoint;

    private enum State
    {
        Idle,
        MovingToStart,
        FollowingSpline,
        FollowingPoints,
        Returning
    }

    private enum PanMode
    {
        Spline,
        PointToPoint
    }

    // =========================================================
    // CAMERA TARGET VALUES
    // =========================================================

    [Header("Camera Target Values")]

    [SerializeField]
    private Vector3 desiredPosition;

    [SerializeField]
    private Quaternion desiredRotation;

    // =========================================================
    // PAN ROTATION
    // =========================================================

    [Header("Pan Rotation")]

    [SerializeField]
    private bool enablePanRotation = true;

    // =========================================================
    // RESET STATIC
    // =========================================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        GlobalPanActive = false;
    }

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (controller == null)
        {
            controller =
                GetComponentInParent<CameraController>();
        }

        if (playerTransform == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerTransform =
                    player.transform;
            }
        }

        if (roundTrigger == null)
        {
            roundTrigger =
                FindAnyObjectByType<CameraPanRoundTrigger>();
        }

        if (doorSystem == null)
        {
            doorSystem =
                FindAnyObjectByType<DoorSystem>();
        }

        FindMissingPanPoints();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        GlobalPanActiveInspector =
            GlobalPanActive;

        if (allowDashSkip &&
            IsPanning &&
            Input.GetButtonDown("Dash"))
        {
            SkipPan();
        }

        if (movingToPausePoint)
        {
            MoveCameraToPausePoint();
        }

        if (returningFromPausePoint)
        {
            ReturnFromPausePoint();
        }
    }

    // =========================================================
    // PAUSE POINT
    // =========================================================

    public void MoveCameraToPausePoint()
    {
        Transform cam = CameraTransform;

        if (cam == null ||
            PAUSEPOINT == null)
        {
            return;
        }

        GlobalPanActive = true;

        movingToPausePoint = true;
        returningFromPausePoint = false;
        atPausePoint = false;

        pausePointMoveTimer +=
            Time.unscaledDeltaTime;

        float t =
            Mathf.Clamp01(
                pausePointMoveTimer / 1f
            );

        cam.position =
            Vector3.Lerp(
                pauseReturnPosition,
                PAUSEPOINT.position,
                t
            );

        cam.rotation =
            Quaternion.Slerp(
                pauseReturnRotation,
                PAUSEPOINT.rotation,
                t
            );

        if (t >= 1f)
        {
            cam.position =
                PAUSEPOINT.position;

            cam.rotation =
                PAUSEPOINT.rotation;

            movingToPausePoint = false;
            atPausePoint = true;

            pausePointMoveTimer = 0f;
        }
    }

    public void StartPausePoint()
    {
        Transform cam = CameraTransform;

        if (cam == null ||
            PAUSEPOINT == null)
        {
            return;
        }

        pauseReturnPosition =
            cam.position;

        pauseReturnRotation =
            cam.rotation;

        pausePointMoveTimer = 0f;

        movingToPausePoint = true;
        returningFromPausePoint = false;
        atPausePoint = false;

        GlobalPanActive = true;
    }

    public void EndPausePoint()
    {
        Transform cam = CameraTransform;

        if (cam == null)
            return;

        movingToPausePoint = false;
        atPausePoint = false;
        returningFromPausePoint = true;

        pausePointMoveTimer = 0f;

        GlobalPanActive = true;
    }

    // =========================================================
    // PAUSE POINT RETURN
    // =========================================================

    private Vector3 pauseReturnStartPosition;
    private Quaternion pauseReturnStartRotation;

    private void ReturnFromPausePoint()
    {
        Transform cam = CameraTransform;

        if (cam == null)
            return;

        // =====================================================
        // START RETURN FROM THE CAMERA'S ACTUAL CURRENT
        // POSITION.
        //
        // IMPORTANT:
        // DO NOT USE PAUSEPOINT.position HERE.
        // The camera may have been moved somewhere else.
        // =====================================================

        if (pausePointMoveTimer <= 0f)
        {
            pauseReturnStartPosition = cam.position;
            pauseReturnStartRotation = cam.rotation;
        }

        // =====================================================
        // RETURN TARGET
        // =====================================================

        Vector3 targetPosition =
            panStart_EndObjects != null
                ? panStart_EndObjects.position
                : pauseReturnPosition;

        Quaternion targetRotation =
            panStart_EndObjects != null
                ? panStart_EndObjects.rotation
                : pauseReturnRotation;

        // =====================================================
        // EXACTLY 1 SECOND
        // =====================================================

        pausePointMoveTimer += Time.unscaledDeltaTime;

        float t =
            Mathf.Clamp01(
                pausePointMoveTimer / 1f
            );

        // Smooth but does NOT snap.
        float easedT = EaseInOutQuad(t);

        // =====================================================
        // MOVE FROM CURRENT CAMERA POSITION
        // TO THE ACTUAL RETURN TARGET
        // =====================================================

        cam.position =
            Vector3.Lerp(
                pauseReturnStartPosition,
                targetPosition,
                easedT
            );

        cam.rotation =
            Quaternion.Slerp(
                pauseReturnStartRotation,
                targetRotation,
                easedT
            );

        desiredPosition = cam.position;
        desiredRotation = cam.rotation;

        // =====================================================
        // FINISHED
        // =====================================================

        if (t >= 1f)
        {
            // Force the exact final position only at the
            // END of the one-second return.
            cam.position = targetPosition;
            cam.rotation = targetRotation;

            desiredPosition = targetPosition;
            desiredRotation = targetRotation;

            returningFromPausePoint = false;
            movingToPausePoint = false;
            atPausePoint = false;

            pausePointMoveTimer = 0f;

            GlobalPanActive = false;
        }
    }

    // =========================================================
    // APPLY EFFECT
    // =========================================================

    public override Vector3 ApplyEffect(float deltaTime)
    {
        GlobalPanActiveInspector =
            GlobalPanActive;

        if (!cameraReady)
        {
            cameraReadyTimer += deltaTime;

            if (cameraReadyTimer <
                cameraStartupDelay)
            {
                return Vector3.zero;
            }

            cameraReady = true;
        }

        Transform cam = CameraTransform;

        if (cam == null)
            return Vector3.zero;

        if (movingToPausePoint ||
            returningFromPausePoint ||
            atPausePoint)
        {
            desiredPosition =
                cam.position;

            desiredRotation =
                cam.rotation;

            if (controller != null)
            {
                return cam.position -
                       controller.GetBasePosition();
            }

            return Vector3.zero;
        }

        if (state == State.Idle)
        {
            return Vector3.zero;
        }

        switch (state)
        {
            case State.MovingToStart:
                MoveCameraToSplineStart(deltaTime);
                break;

            case State.FollowingSpline:
                FollowSpline(deltaTime);
                break;

            case State.FollowingPoints:
                FollowPoints(deltaTime);
                break;

            case State.Returning:
                ReturnCamera(deltaTime);
                break;
        }

        desiredPosition =
            cam.position;

        desiredRotation =
            cam.rotation;

        if (controller != null)
        {
            return cam.position -
                   controller.GetBasePosition();
        }

        return Vector3.zero;
    }

    // =========================================================
    // MOVE TO SPLINE START
    // =========================================================
    //
    // IMPORTANT:
    //
    // This is ONLY done once at the beginning.
    //
    // Once FollowingSpline starts, the camera is controlled
    // ONLY by EvaluatePosition on the spline.
    //
    // =========================================================

    private void MoveCameraToSplineStart(float deltaTime)
    {
        Transform cam = CameraTransform;

        if (cam == null)
            return;

        if (activeSpline == null ||
            activeSpline.Spline == null ||
            activeSpline.Spline.Count < 2)
        {
            StartPointToPointFallback();
            return;
        }

        Vector3 splineStart =
            activeSpline.transform.TransformPoint(
                activeSpline.Spline.EvaluatePosition(0f)
            );

        debugDistanceToTravel =
            Vector3.Distance(
                cam.position,
                splineStart
            );

        float distanceThisFrame =
            Mathf.Max(
                0f,
                moveToStartSpeed
            ) * deltaTime;

        debugDistanceTravelled +=
            distanceThisFrame;

        float distance =
            Vector3.Distance(
                cam.position,
                splineStart
            );

        if (distance <= 0.001f)
        {
            cam.position = splineStart;

            splineDistance = 0f;
            splineProgressDebug = 0f;

            currentSplinePointIndex = 0;

            splinePointHoldTimer = 0f;
            holdingAtSplinePoint = false;

            state = State.FollowingSpline;

            return;
        }

        cam.position =
            Vector3.MoveTowards(
                cam.position,
                splineStart,
                distanceThisFrame
            );

        if (enablePanRotation)
        {
            Vector3 direction =
                splineStart - cam.position;

            if (direction.sqrMagnitude >
                0.000001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up
                    );

                cam.rotation =
                    Quaternion.RotateTowards(
                        cam.rotation,
                        targetRotation,
                        rotationSpeed * deltaTime
                    );
            }
        }

        distance =
            Vector3.Distance(
                cam.position,
                splineStart
            );

        if (distance <= 0.001f)
        {
            cam.position = splineStart;

            splineDistance = 0f;
            splineProgressDebug = 0f;

            currentSplinePointIndex = 0;

            splinePointHoldTimer = 0f;
            holdingAtSplinePoint = false;

            state = State.FollowingSpline;
        }
    }

    // =========================================================
    // FOLLOW SPLINE
    // =========================================================
    //
    // THIS IS THE IMPORTANT FIX.
    //
    // The camera NEVER gets snapped to a knot when
    // holdTime == 0.
    //
    // The spline itself controls the camera position.
    //
    // PanPoints ONLY provide hold times.
    //
    // =========================================================

    private void FollowSpline(float deltaTime)
    {
        Transform cam = CameraTransform;

        if (cam == null)
            return;

        if (activeSpline == null ||
            activeSpline.Spline == null)
        {
            StartPointToPointFallback();
            return;
        }

        int knotCount =
            activeSpline.Spline.Count;

        if (knotCount < 2)
        {
            StartReturning();
            return;
        }

        if (splineLength <= 0.0001f)
        {
            splineLength =
                activeSpline.Spline.GetLength();
        }

        // =====================================================
        // HOLDING AT A KNOT
        // =====================================================

        if (holdingAtSplinePoint)
        {
            float holdTime =
                GetSplinePointHoldTime(
                    currentSplinePointIndex
                );

            // =================================================
            // ZERO HOLD
            // =================================================
            //
            // IMPORTANT:
            //
            // If hold time is zero, NEVER force the camera
            // to the knot and NEVER start a hold.
            //
            // Continue immediately.
            //
            // =================================================

            if (holdTime <= 0.000001f)
            {
                holdingAtSplinePoint = false;
                splinePointHoldTimer = 0f;

                currentSplinePointIndex++;

                if (currentSplinePointIndex >= knotCount)
                {
                    MarkDoorReady();
                    StartReturning();
                    return;
                }

                return;
            }

            // =================================================
            // POSITIVE HOLD
            // =================================================

            Vector3 knotPosition =
                GetSplineKnotWorldPosition(
                    currentSplinePointIndex
                );

            // Only a REAL hold is allowed to place the camera
            // exactly on the knot.
            cam.position =
                knotPosition;

            desiredPosition =
                knotPosition;

            splinePointHoldTimer +=
                deltaTime;

            if (splinePointHoldTimer <
                holdTime)
            {
                return;
            }

            splinePointHoldTimer = 0f;

            holdingAtSplinePoint = false;

            currentSplinePointIndex++;

            if (currentSplinePointIndex >= knotCount)
            {
                MarkDoorReady();
                StartReturning();
            }

            return;
        }

        // =====================================================
        // MOVE ALONG SPLINE
        // =====================================================
        //
        // We advance by distance.
        //
        // We do NOT calculate knot progress using:
        //
        // knotIndex / (knotCount - 1)
        //
        // because knots are not necessarily evenly distributed.
        //
        // =====================================================

        float previousDistance =
            splineDistance;

        float distanceThisFrame =
            Mathf.Max(
                0f,
                moveSpeed
            ) * deltaTime;

        splineDistance +=
            distanceThisFrame;

        splineDistance =
            Mathf.Min(
                splineDistance,
                splineLength
            );

        // =====================================================
        // CHECK WHETHER A POSITIVE-HOLD KNOT WAS CROSSED
        // =====================================================

        while (currentSplinePointIndex < knotCount)
        {
            float knotDistance =
                GetSplineKnotDistance(
                    currentSplinePointIndex
                );

            float holdTime =
                GetSplinePointHoldTime(
                    currentSplinePointIndex
                );

            // =================================================
            // ZERO HOLD:
            //
            // DO ABSOLUTELY NOTHING.
            //
            // This is the key fix.
            // =================================================

            if (holdTime <= 0.000001f)
            {
                currentSplinePointIndex++;

                continue;
            }

            // =================================================
            // POSITIVE HOLD KNOT
            // =================================================

            if (splineDistance >= knotDistance)
            {
                // We have crossed the knot.

                // Move the spline distance exactly to the knot
                // BEFORE beginning the hold.
                //
                // This prevents the camera from being moved
                // backwards by a later spline evaluation.

                splineDistance =
                    knotDistance;

                Vector3 knotPosition =
                    GetSplineKnotWorldPosition(
                        currentSplinePointIndex
                    );

                cam.position =
                    knotPosition;

                desiredPosition =
                    knotPosition;

                holdingAtSplinePoint = true;

                splinePointHoldTimer = 0f;

                return;
            }

            break;
        }

        // =====================================================
        // END OF SPLINE
        // =====================================================

        if (splineDistance >= splineLength - 0.000001f)
        {
            splineDistance =
                splineLength;

            Vector3 endPosition =
                activeSpline.transform.TransformPoint(
                    activeSpline.Spline.EvaluatePosition(1f)
                );

            cam.position =
                endPosition;

            desiredPosition =
                endPosition;

            // The final knot can have a hold.
            if (currentSplinePointIndex <
                knotCount)
            {
                float finalHold =
                    GetSplinePointHoldTime(
                        knotCount - 1
                    );

                if (finalHold > 0.000001f)
                {
                    currentSplinePointIndex =
                        knotCount - 1;

                    splinePointHoldTimer = 0f;

                    holdingAtSplinePoint = true;

                    return;
                }
            }

            MarkDoorReady();

            StartReturning();

            return;
        }

        // =====================================================
        // NORMAL SPLINE POSITION
        // =====================================================
        //
        // ONLY THE SPLINE IS ALLOWED TO SET POSITION HERE.
        //
        // No PanPoint transform.
        // No knot snapping.
        // No correction to a knot.
        //
        // =====================================================

        float progress =
            Mathf.Clamp01(
                splineDistance /
                splineLength
            );

        splineProgressDebug =
            progress;

        Vector3 localPosition =
            activeSpline.Spline.EvaluatePosition(
                progress
            );

        Vector3 worldPosition =
            activeSpline.transform.TransformPoint(
                localPosition
            );

        cam.position =
            worldPosition;

        desiredPosition =
            worldPosition;

        // =====================================================
        // ROTATION
        // =====================================================

        if (enablePanRotation)
        {
            Vector3 localTangent =
                activeSpline.Spline.EvaluateTangent(
                    progress
                );

            Vector3 worldTangent =
                activeSpline.transform.TransformDirection(
                    localTangent
                );

            if (worldTangent.sqrMagnitude >
                0.000001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        worldTangent.normalized,
                        Vector3.up
                    );

                cam.rotation =
                    Quaternion.RotateTowards(
                        cam.rotation,
                        targetRotation,
                        rotationSpeed * deltaTime
                    );

                desiredRotation =
                    cam.rotation;
            }
        }
    }

    // =========================================================
    // GET SPLINE KNOT WORLD POSITION
    // =========================================================

    private Vector3 GetSplineKnotWorldPosition(
        int index)
    {
        if (activeSpline == null ||
            activeSpline.Spline == null)
        {
            return Vector3.zero;
        }

        int knotCount =
            activeSpline.Spline.Count;

        if (knotCount == 0)
            return Vector3.zero;

        index =
            Mathf.Clamp(
                index,
                0,
                knotCount - 1
            );

        BezierKnot knot =
            activeSpline.Spline[index];

        return activeSpline.transform.TransformPoint(
            knot.Position
        );
    }

    // =========================================================
    // GET SPLINE KNOT DISTANCE
    // =========================================================
    //
    // Converts a knot's spline parameter into distance.
    //
    // This is much safer than assuming every knot is evenly
    // spaced along the spline.
    //
    // =========================================================

    private float GetSplineKnotDistance(int knotIndex)
    {
        if (activeSpline == null ||
            activeSpline.Spline == null)
        {
            return 0f;
        }

        int knotCount =
            activeSpline.Spline.Count;

        if (knotCount <= 1)
            return 0f;

        knotIndex =
            Mathf.Clamp(
                knotIndex,
                0,
                knotCount - 1
            );

        if (knotIndex == 0)
            return 0f;

        float knotT =
            activeSpline.Spline.ConvertIndexUnit(
                knotIndex,
                PathIndexUnit.Knot,
                PathIndexUnit.Normalized
            );

        float distance =
            activeSpline.Spline.ConvertIndexUnit(
                knotT,
                PathIndexUnit.Normalized,
                PathIndexUnit.Distance
            );

        return Mathf.Clamp(
            distance,
            0f,
            splineLength
        );
    }

    // =========================================================
    // GET SPLINE HOLD TIME
    // =========================================================

    private float GetSplinePointHoldTime(
        int index)
    {
        if (splineHoldTimes == null)
            return 0f;

        if (index < 0 ||
            index >= splineHoldTimes.Count)
        {
            return 0f;
        }

        return Mathf.Max(
            0f,
            splineHoldTimes[index]
        );
    }

    // =========================================================
    // FOLLOW POINTS
    // =========================================================

    private void FollowPoints(float deltaTime)
    {
        Transform cam = CameraTransform;

        if (cam == null)
            return;

        if (pointPath == null ||
            pointPath.Count < 2)
        {
            StartReturning();
            return;
        }

        if (currentPointIndex < 1)
        {
            currentPointIndex = 1;
        }

        if (currentPointIndex >=
            pointPath.Count)
        {
            MarkDoorReady();

            StartReturning();

            return;
        }

        Vector3 target =
            pointPath[currentPointIndex];

        // =====================================================
        // HOLD
        // =====================================================

        if (holdingAtPoint)
        {
            cam.position =
                target;

            desiredPosition =
                target;

            pointHoldTimer +=
                deltaTime;

            float holdTime =
                GetCurrentPointHoldTime();

            if (pointHoldTimer <
                holdTime)
            {
                return;
            }

            pointHoldTimer = 0f;

            holdingAtPoint = false;

            currentPointIndex++;

            if (currentPointIndex >=
                pointPath.Count)
            {
                MarkDoorReady();

                StartReturning();

                return;
            }

            return;
        }

        // =====================================================
        // MOVE
        // =====================================================

        cam.position =
            Vector3.MoveTowards(
                cam.position,
                target,
                moveSpeed * deltaTime
            );

        desiredPosition =
            cam.position;

        // =====================================================
        // ROTATION
        // =====================================================

        if (enablePanRotation)
        {
            Vector3 direction =
                target - cam.position;

            if (direction.sqrMagnitude >
                0.000001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up
                    );

                cam.rotation =
                    Quaternion.RotateTowards(
                        cam.rotation,
                        targetRotation,
                        rotationSpeed * deltaTime
                    );

                desiredRotation =
                    cam.rotation;
            }
        }

        // =====================================================
        // REACHED POINT
        // =====================================================

        if (Vector3.Distance(
                cam.position,
                target
            ) <= 0.01f)
        {
            cam.position =
                target;

            pointHoldTimer = 0f;

            holdingAtPoint = true;
        }
    }

    // =========================================================
    // GET POINT HOLD TIME
    // =========================================================

    private float GetCurrentPointHoldTime()
    {
        int index =
            currentPointIndex - 1;

        if (index < 0 ||
            index >= panPoints.Count)
        {
            return 0f;
        }

        if (panPoints[index] == null)
        {
            return 0f;
        }

        return Mathf.Max(
            0f,
            panPoints[index].holdTime
        );
    }

    // =========================================================
    // BUILD POINT PATH
    // =========================================================

    private bool BuildPointPath(
        List<CameraPanRoundTrigger.PanPoint> points)
    {
        Transform cam = CameraTransform;

        pointPath.Clear();

        panPoints.Clear();

        currentPointIndex = 0;

        pointInitialized = false;

        pointHoldTimer = 0f;

        holdingAtPoint = false;

        if (cam == null)
            return false;

        pointPath.Add(
            cam.position
        );

        if (points == null ||
            points.Count == 0)
        {
            return false;
        }

        foreach (
            CameraPanRoundTrigger.PanPoint panPoint
            in points)
        {
            if (panPoint == null)
                continue;

            if (panPoint.pointOfInterest == null)
                continue;

            Vector3 target =
                panPoint.pointOfInterest.position;

            target +=
                Vector3.up *
                pointHeightOffset;

            pointPath.Add(
                target
            );

            PanPoint storedPoint =
                new PanPoint();

            storedPoint.pointPosition =
                target;

            storedPoint.holdTime =
                Mathf.Max(
                    0f,
                    panPoint.holdTime
                );

            panPoints.Add(
                storedPoint
            );
        }

        pointInitialized =
            pointPath.Count >= 2;

        return pointInitialized;
    }

    // =========================================================
    // PREPARE SPLINE HOLD TIMES
    // =========================================================
    //
    // ONLY HOLD TIMES COME FROM PAN POINTS.
    //
    // The PanPoint positions are NOT used for spline
    // positioning.
    //
    // =========================================================

    private void PrepareSplineHoldTimes(
        List<CameraPanRoundTrigger.PanPoint> points)
    {
        splineHoldTimes.Clear();

        if (activeSpline == null ||
            activeSpline.Spline == null)
        {
            return;
        }

        int knotCount =
            activeSpline.Spline.Count;

        for (int i = 0;
             i < knotCount;
             i++)
        {
            float holdTime = 0f;

            if (points != null &&
                i < points.Count)
            {
                CameraPanRoundTrigger.PanPoint source =
                    points[i];

                if (source != null)
                {
                    holdTime =
                        Mathf.Max(
                            0f,
                            source.holdTime
                        );
                }
            }

            splineHoldTimes.Add(
                holdTime
            );
        }
    }

    // =========================================================
    // TRIGGER PAN
    // =========================================================

    public void TriggerPan(
        List<CameraPanRoundTrigger.PanPoint> points,
        float time,
        int doorIndex,
        int round,
        bool usefixedtime)
    {
        ThisPansReturnTime =
            usefixedtime;

        Transform cam =
            CameraTransform;

        if (cam == null)
        {
            Debug.LogWarning(
                "CameraPanEffect: CameraTransform is null."
            );

            return;
        }

        // =====================================================
        // CANCEL PAUSE
        // =====================================================

        movingToPausePoint = false;
        returningFromPausePoint = false;
        atPausePoint = false;

        // =====================================================
        // SAVE RETURN
        // =====================================================

        returnPosition =
            cam.position;

        returnRotation =
            cam.rotation;

        // =====================================================
        // RESET
        // =====================================================

        doorIndexToReady =
            doorIndex;

        doorReadyTriggered =
            false;

        activePanPoints =
            points;

        pointPath.Clear();

        panPoints.Clear();

        splineHoldTimes.Clear();

        currentPointIndex = 0;
        currentSplinePointIndex = 0;

        pointInitialized = false;

        pointHoldTimer = 0f;
        holdingAtPoint = false;

        splinePointHoldTimer = 0f;
        holdingAtSplinePoint = false;

        splineDistance = 0f;
        splineLength = 0f;

        splineProgressDebug = 0f;

        debugDistanceTravelled = 0f;
        debugDistanceToTravel = 0f;

        returnTimer = 0f;

        activeSpline = null;

        // =====================================================
        // FIND ROUND TRIGGER
        // =====================================================

        if (roundTrigger == null)
        {
            roundTrigger =
                FindAnyObjectByType<CameraPanRoundTrigger>();
        }

        // =====================================================
        // FIND SPLINE
        // =====================================================

        if (roundTrigger != null)
        {
            activeSpline =
                roundTrigger.GetSplineForRound(
                    round
                );
        }

        // =====================================================
        // SPLINE MODE
        // =====================================================

        if (activeSpline != null &&
            activeSpline.Spline != null &&
            activeSpline.Spline.Count >= 2)
        {
            panMode =
                PanMode.Spline;

            splineLength =
                activeSpline.Spline.GetLength();

            splineDistance = 0f;

            // ONLY COPY HOLD TIMES.
            PrepareSplineHoldTimes(
                points
            );

            currentSplinePointIndex = 0;

            splinePointHoldTimer = 0f;

            holdingAtSplinePoint = false;

            state =
                State.MovingToStart;

            Debug.Log(
                $"CameraPanEffect: SPLINE mode. " +
                $"Knots = {activeSpline.Spline.Count}. " +
                $"Hold times = {splineHoldTimes.Count}."
            );
        }

        // =====================================================
        // POINT TO POINT MODE
        // =====================================================

        else
        {
            panMode =
                PanMode.PointToPoint;

            bool validPath =
                BuildPointPath(
                    points
                );

            if (!validPath)
            {
                Debug.LogWarning(
                    "CameraPanEffect: No valid spline " +
                    "and no valid point-to-point path."
                );

                state =
                    State.Idle;

                return;
            }

            currentPointIndex = 1;

            pointHoldTimer = 0f;

            holdingAtPoint = false;

            state =
                State.FollowingPoints;
        }

        // =====================================================
        // PAN ACTIVE
        // =====================================================

        GlobalPanActive =
            true;

        // =====================================================
        // PLAYER
        // =====================================================

        if (pausePlayerDuringPan &&
            !playerPaused)
        {
            playerPaused = true;
        }
    }

    // =========================================================
    // POINT FALLBACK
    // =========================================================

    private void StartPointToPointFallback()
    {
        if (pointPath != null &&
            pointPath.Count >= 2)
        {
            currentPointIndex = 1;

            pointHoldTimer = 0f;

            holdingAtPoint = false;

            state =
                State.FollowingPoints;

            panMode =
                PanMode.PointToPoint;

            return;
        }

        StartReturning();
    }

    // =========================================================
    // START RETURN
    // =========================================================

    private void StartReturning()
    {
        returnTimer = 0f;

        state =
            State.Returning;
    }

    // =========================================================
    // RETURN CAMERA
    // =========================================================

    private void ReturnCamera(float deltaTime)
    {
        Transform cam = CameraTransform;

        if (cam == null)
        {
            state = State.Idle;
            ResumePlayer();
            return;
        }

        // =====================================================
        // FIXED TIME RETURN
        // =====================================================

        if (ThisPansReturnTime)
        {
            if (returnTimer <= 0f)
            {
                returnStartPosition =
                    cam.position;

                returnStartRotation =
                    cam.rotation;
            }

            float duration =
                Mathf.Max(
                    0.01f,
                    returnTime
                );

            returnTimer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    returnTimer / duration
                );

            float easedT =
                EaseInOutQuad(t);

            cam.position =
                Vector3.Lerp(
                    returnStartPosition,
                    returnPosition,
                    easedT
                );

            cam.rotation =
                Quaternion.Slerp(
                    returnStartRotation,
                    returnRotation,
                    easedT
                );

            desiredPosition =
                cam.position;

            desiredRotation =
                cam.rotation;

            if (t >= 1f)
            {
                FinishPanReturn();
            }

            return;
        }

        // =====================================================
        // NORMAL RETURN
        // =====================================================

        cam.position =
            Vector3.MoveTowards(
                cam.position,
                returnPosition,
                moveSpeed * deltaTime
            );

        cam.rotation =
            Quaternion.RotateTowards(
                cam.rotation,
                returnRotation,
                rotationSpeed * deltaTime
            );

        desiredPosition =
            cam.position;

        desiredRotation =
            cam.rotation;

        if (Vector3.Distance(
                cam.position,
                returnPosition
            ) <= 0.01f &&
            Quaternion.Angle(
                cam.rotation,
                returnRotation
            ) <= 0.1f)
        {
            FinishPanReturn();
        }
    }

    // =========================================================
    // FINISH RETURN
    // =========================================================

    private void FinishPanReturn()
    {
        Transform cam = CameraTransform;

        if (cam != null)
        {
            cam.position =
                returnPosition;

            cam.rotation =
                returnRotation;
        }

        desiredPosition =
            returnPosition;

        desiredRotation =
            returnRotation;

        returnTimer = 0f;

        state =
            State.Idle;

        activeSpline = null;

        pointPath.Clear();

        panPoints.Clear();

        splineHoldTimes.Clear();

        activePanPoints = null;

        currentPointIndex = 0;

        currentSplinePointIndex = 0;

        pointInitialized = false;

        splineDistance = 0f;

        splineLength = 0f;

        splineProgressDebug = 0f;

        pointHoldTimer = 0f;

        splinePointHoldTimer = 0f;

        holdingAtPoint = false;

        holdingAtSplinePoint = false;

        debugDistanceToTravel = 0f;

        debugDistanceTravelled = 0f;

        ResumePlayer();
    }

    // =========================================================
    // DASH SKIP
    // =========================================================

    public void SkipPan()
    {
        if (movingToPausePoint)
            return;

        if (returningFromPausePoint)
            return;

        if (atPausePoint)
            return;

        if (state == State.Idle)
            return;

        MarkDoorReady();

        Transform cam =
            CameraTransform;

        if (cam != null)
        {
            cam.position =
                returnPosition;

            cam.rotation =
                returnRotation;
        }

        desiredPosition =
            returnPosition;

        desiredRotation =
            returnRotation;

        activeSpline = null;

        pointPath.Clear();

        panPoints.Clear();

        splineHoldTimes.Clear();

        activePanPoints = null;

        currentPointIndex = 0;

        currentSplinePointIndex = 0;

        pointInitialized = false;

        splineDistance = 0f;

        splineLength = 0f;

        splineProgressDebug = 0f;

        pointHoldTimer = 0f;

        splinePointHoldTimer = 0f;

        holdingAtPoint = false;

        holdingAtSplinePoint = false;

        debugDistanceToTravel = 0f;

        debugDistanceTravelled = 0f;

        returnTimer = 0f;

        state =
            State.Idle;

        ResumePlayer();
    }

    // =========================================================
    // DOOR
    // =========================================================

    private void MarkDoorReady()
    {
        if (doorReadyTriggered)
            return;

        if (doorSystem != null &&
            doorIndexToReady >= 0)
        {
            doorSystem.SetDoorReady(
                doorIndexToReady
            );

            doorIndexToReady = -1;
        }

        doorReadyTriggered = true;
    }

    // =========================================================
    // PLAYER
    // =========================================================

    private void ResumePlayer()
    {
        playerPaused = false;

        GlobalPanActive = false;
    }

    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public bool IsPanning
    {
        get
        {
            return
                state != State.Idle ||
                movingToPausePoint ||
                returningFromPausePoint ||
                atPausePoint;
        }
    }

    // =========================================================
    // EASING
    // =========================================================

    private static float EaseInOutQuad(float t)
    {
        if (t < 0.5f)
        {
            return 2f * t * t;
        }

        return -1f +
               (4f - 2f * t) * t;
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    [Header("Gizmo Point References")]

    [SerializeField]
    private List<Transform> panPointsForGizmos =
        new List<Transform>();

    private void OnDrawGizmos()
    {
        if (PAUSEPOINT != null)
        {
            Gizmos.color =
                Color.magenta;

            Gizmos.DrawSphere(
                PAUSEPOINT.position,
                0.75f
            );

            Gizmos.color =
                Color.white;

            Gizmos.DrawLine(
                PAUSEPOINT.position,
                PAUSEPOINT.position +
                PAUSEPOINT.forward * 3f
            );
        }

        if (panStart_EndObjects != null)
        {
            Gizmos.color =
                Color.green;

            Gizmos.DrawSphere(
                panStart_EndObjects.position,
                0.5f
            );
        }

        if (panPointsForGizmos == null ||
            panPointsForGizmos.Count == 0)
        {
            return;
        }

        for (
            int i = 0;
            i < panPointsForGizmos.Count;
            i++)
        {
            Transform point =
                panPointsForGizmos[i];

            if (point == null)
                continue;

            Gizmos.color =
                Color.yellow;

            Gizmos.DrawSphere(
                point.position,
                0.5f
            );

            if (i + 1 <
                panPointsForGizmos.Count)
            {
                Transform next =
                    panPointsForGizmos[i + 1];

                if (next != null)
                {
                    Gizmos.color =
                        Color.cyan;

                    Gizmos.DrawLine(
                        point.position,
                        next.position
                    );
                }
            }
        }
    }
}