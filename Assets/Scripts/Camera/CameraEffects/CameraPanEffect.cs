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

    [Tooltip("Speed used for spline and point-to-point movement.")]
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
    [SerializeField]
    private float pausePointMoveTimer = 0f;


    // =========================================================
    // MOVE TO PAUSE POINT — EXACTLY 1 SECOND
    // =========================================================

    public void MoveCameraToPausePoint()
    {
        Transform cam = CameraTransform;

        if (cam == null || PAUSEPOINT == null)
            return;

        GlobalPanActive = true;

        movingToPausePoint = true;
        returningFromPausePoint = false;
        atPausePoint = false;

        // Start the 1 second timer.
        if (pausePointMoveTimer <= 0f)
        {
            pausePointMoveTimer = 0f;
        }

        pausePointMoveTimer += Time.unscaledDeltaTime;

        float t = Mathf.Clamp01(pausePointMoveTimer / 1f);

        // IMPORTANT:
        // This moves directly toward the PAUSEPOINT GameObject.
        cam.position = Vector3.Lerp(
            cam.position,
            PAUSEPOINT.position,
            t
        );

        // IMPORTANT:
        // Match the PAUSEPOINT rotation.
        cam.rotation = Quaternion.Slerp(
            cam.rotation,
            PAUSEPOINT.rotation,
            t
        );

        // Finished.
        if (t >= 1f)
        {
            cam.position = PAUSEPOINT.position;
            cam.rotation = PAUSEPOINT.rotation;

            movingToPausePoint = false;
            atPausePoint = true;

            pausePointMoveTimer = 0f;
        }
    }

    // =========================================================
    // END PAUSE POINT
    // =========================================================

    public void EndPausePoint()
    {
        Transform cam = CameraTransform;

        if (cam == null || panStart_EndObjects == null)
            return;

        movingToPausePoint = false;
        atPausePoint = false;

        returningFromPausePoint = true;

        pausePointMoveTimer = 0f;

        GlobalPanActive = true;
    }


    // =========================================================
    // RETURN TO THE GAMEOBJECT — EXACTLY 1 SECOND
    // =========================================================

    private void ReturnFromPausePoint()
    {
        Transform cam = CameraTransform;

        if (cam == null || panStart_EndObjects == null)
            return;

        pausePointMoveTimer += Time.unscaledDeltaTime;

        float t = Mathf.Clamp01(pausePointMoveTimer / 1f);

        cam.position = Vector3.Lerp(
            cam.position,
            panStart_EndObjects.position,
            t
        );

        cam.rotation = Quaternion.Slerp(
            cam.rotation,
            panStart_EndObjects.rotation,
            t
        );

        if (t >= 1f)
        {
            cam.position = panStart_EndObjects.position;
            cam.rotation = panStart_EndObjects.rotation;

            returningFromPausePoint = false;
            movingToPausePoint = false;
            atPausePoint = false;

            pausePointMoveTimer = 0f;

            GlobalPanActive = false;
        }
    }
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

    [Tooltip("Camera controller that normally controls the camera.")]
    [SerializeField]
    private CameraController controller;

    [Tooltip("Player transform used for returning camera control.")]
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

    [Tooltip("Camera moves to this transform and exactly matches its rotation.")]
    [SerializeField]
    private Transform PAUSEPOINT;

    [Tooltip("Fixed movement speed to and from the pause point. Does NOT use TriggerPan time.")]
    [SerializeField]
    private float pausePointMoveSpeed = 20f;

    [Tooltip("Fixed rotation speed to and from the pause point.")]
    [SerializeField]
    private float pausePointRotationSpeed = 180f;

    [Tooltip("Camera is currently moving toward PAUSEPOINT.")]
    [SerializeField]
    private bool movingToPausePoint;

    [Tooltip("Camera is currently returning from PAUSEPOINT.")]
    [SerializeField]
    private bool returningFromPausePoint;

    [Tooltip("Camera has reached PAUSEPOINT and is waiting.")]
    [SerializeField]
    public bool atPausePoint;

    [Tooltip("Position the camera had before entering the pause point.")]
    [SerializeField]
    private Vector3 pauseReturnPosition;

    [Tooltip("Rotation the camera had before entering the pause point.")]
    [SerializeField]
    private Quaternion pauseReturnRotation;


    // =========================================================
    // SPLINE
    // =========================================================

    [Header("Spline")]

    [SerializeField]
    private CameraPanRoundTrigger roundTrigger;


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
    // SPLINE RUNTIME
    // =========================================================

    [Header("Spline Runtime")]

    [SerializeField]
    private SplineContainer activeSpline;

    [SerializeField]
    private float splineDistance;

    [SerializeField]
    private float splineLength;


    // =========================================================
    // SPLINE START MOVEMENT DEBUG
    // =========================================================

    [Header("Spline Start Movement Debug")]

    [SerializeField]
    private float splineKnotReachDistance = 0.01f;

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
    private float pointSegmentTimer;

    [SerializeField]
    private float pointSegmentTime;


    // =========================================================
    // POINT DATA
    // =========================================================

    [System.Serializable]
    public class PanPoint
    {
        public Vector3 pointPosition;

        public float holdTime = 5f;
    }


    [SerializeField]
    private List<PanPoint> panPoints =
        new List<PanPoint>();


    private List<CameraPanRoundTrigger.PanPoint> activePanPoints;


    // =========================================================
    // RETURN CAMERA
    // =========================================================

    [Header("Return Camera")]

    [SerializeField]
    private Vector3 returnPosition;

    [SerializeField]
    private Quaternion returnRotation;


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
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        GlobalPanActiveInspector =
            GlobalPanActive;

        // =====================================================
        // DASH SKIP
        // =====================================================

        if (allowDashSkip &&
            IsPanning &&
            Input.GetButtonDown("Dash"))
        {
            SkipPan();
        }

        // =====================================================
        // PAUSE POINT MOVEMENT
        // =====================================================

        if (movingToPausePoint)
        {
            MoveCameraToPausePoint();
        }

        // =====================================================
        // PAUSE POINT RETURN
        // =====================================================

        if (returningFromPausePoint)
        {
            ReturnFromPausePoint();
        }
    }


    // =========================================================
    // APPLY EFFECT
    // =========================================================

    public override Vector3 ApplyEffect(float deltaTime)
    {
        GlobalPanActiveInspector =
            GlobalPanActive;


        // =====================================================
        // CAMERA STARTUP
        // =====================================================

        if (!cameraReady)
        {
            cameraReadyTimer += deltaTime;

            if (cameraReadyTimer < cameraStartupDelay)
            {
                return Vector3.zero;
            }

            cameraReady = true;
        }


        // =====================================================
        // CAMERA
        // =====================================================

        Transform cam = CameraTransform;

        if (cam == null)
        {
            return Vector3.zero;
        }


        // =====================================================
        // PAUSE POINT
        // =====================================================

        if (movingToPausePoint ||
            returningFromPausePoint ||
            atPausePoint)
        {
            desiredPosition = cam.position;
            desiredRotation = cam.rotation;

            if (controller != null)
            {
                return cam.position -
                       controller.GetBasePosition();
            }

            return Vector3.zero;
        }


        // =====================================================
        // IDLE
        // =====================================================

        if (state == State.Idle)
        {
            return Vector3.zero;
        }


        // =====================================================
        // NORMAL PAN
        // =====================================================

        if (state == State.MovingToStart)
        {
            MoveCameraToSplineStart(deltaTime);
        }
        else if (state == State.FollowingSpline)
        {
            FollowSpline(deltaTime);
        }
        else if (state == State.FollowingPoints)
        {
            FollowPoints(deltaTime);
        }
        else if (state == State.Returning)
        {
            ReturnCamera(deltaTime);
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
    // SPLINE START
    // =========================================================

    // =========================================================
    // MOVE CAMERA TO SPLINE START
    // =========================================================

    private void MoveCameraToSplineStart(float deltaTime)
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


        // =====================================================
        // SPLINE START POSITION
        // =====================================================

        Vector3 splineStart =
            activeSpline.transform.TransformPoint(
                activeSpline.Spline.EvaluatePosition(0f)
            );


        // =====================================================
        // DEBUG DISTANCE
        // =====================================================

        debugDistanceToTravel =
            Vector3.Distance(
                cam.position,
                splineStart
            );


        debugDistanceTravelled +=
            moveToStartSpeed * deltaTime;


        // =====================================================
        // MOVE CAMERA
        // =====================================================

        cam.position =
            Vector3.MoveTowards(
                cam.position,
                splineStart,
                moveToStartSpeed * deltaTime
            );


        // =====================================================
        // ROTATION
        // =====================================================

        if (enablePanRotation)
        {
            Vector3 direction =
                splineStart -
                cam.position;


            if (direction.sqrMagnitude > 0.001f)
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
        // REACHED SPLINE START
        // =====================================================

        float distance =
            Vector3.Distance(
                cam.position,
                splineStart
            );


        if (distance <= splineKnotReachDistance)
        {
            cam.position =
                splineStart;


            splineDistance =
                0f;


            debugDistanceTravelled =
                debugDistanceToTravel;


            state =
                State.FollowingSpline;
        }
    }


    // =========================================================
    // FOLLOW SPLINE
    // =========================================================

    // =========================================================
    // FOLLOW SPLINE
    // =========================================================
    [Header("Pan Rotation")]

    [SerializeField]
    private bool enablePanRotation = true;
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


        if (splineLength <= 0.001f)
        {
            splineLength =
                activeSpline.Spline.GetLength();
        }


        // =====================================================
        // MOVE ALONG SPLINE
        // =====================================================

        splineDistance +=
            moveSpeed * deltaTime;


        float progress =
            Mathf.Clamp01(
                splineDistance /
                splineLength
            );


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


            if (worldTangent.sqrMagnitude > 0.001f)
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


        // =====================================================
        // SPLINE FINISHED
        // =====================================================

        if (progress >= 1f)
        {
            MarkDoorReady();

            StartReturning();
        }
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


        if (currentPointIndex >= pointPath.Count)
        {
            MarkDoorReady();

            StartReturning();

            return;
        }


        // =====================================================
        // CURRENT TARGET
        // =====================================================

        Vector3 target =
            pointPath[currentPointIndex];


        // =====================================================
        // MOVE CAMERA
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
                target -
                cam.position;


            if (direction.sqrMagnitude > 0.001f)
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


            currentPointIndex++;


            // =================================================
            // MORE POINTS
            // =================================================

            if (currentPointIndex <
                pointPath.Count)
            {
                return;
            }


            // =================================================
            // ALL POINTS FINISHED
            // =================================================

            MarkDoorReady();

            StartReturning();
        }
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


        if (cam == null)
        {
            return false;
        }


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
            {
                continue;
            }

            if (panPoint.pointOfInterest == null)
            {
                continue;
            }


            Vector3 target =
                panPoint.pointOfInterest.position;


            target +=
                Vector3.up *
                pointHeightOffset;


            pointPath.Add(target);


            PanPoint storedPoint =
                new PanPoint();


            storedPoint.pointPosition =
                target;


            storedPoint.holdTime =
                panPoint.holdTime;


            panPoints.Add(
                storedPoint
            );
        }


        pointInitialized =
            pointPath.Count >= 2;


        return pointInitialized;
    }


    // =========================================================
    // TRIGGER PAN
    // =========================================================

    public void TriggerPan(
        List<CameraPanRoundTrigger.PanPoint> points,
        float time,
        int doorIndex,
        int round)
    {
        Transform cam = CameraTransform;

        if (cam == null)
        {
            Debug.LogWarning(
                "CameraPanEffect: CameraTransform is null."
            );

            return;
        }


        // =====================================================
        // CANCEL PAUSE POINT
        // =====================================================

        movingToPausePoint = false;
        returningFromPausePoint = false;
        atPausePoint = false;


        // =====================================================
        // SAVE RETURN POSITION
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

        pointPath.Clear();

        panPoints.Clear();

        currentPointIndex = 0;

        pointInitialized = false;

        pointSegmentTimer = 0f;

        pointSegmentTime = 0f;

        splineDistance = 0f;

        splineLength = 0f;

        debugDistanceTravelled = 0f;

        debugDistanceToTravel = 0f;

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
        // SPLINE
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


            state =
                State.MovingToStart;


            Debug.Log(
                "CameraPanEffect: Using SPLINE."
            );
        }


        // =====================================================
        // POINT TO POINT
        // =====================================================

        else
        {
            panMode =
                PanMode.PointToPoint;


            activePanPoints =
                points;


            bool validPath =
                BuildPointPath(
                    points
                );


            if (!validPath)
            {
                Debug.LogWarning(
                    "CameraPanEffect: No valid spline and no valid point-to-point path."
                );


                state =
                    State.Idle;


                return;
            }


            currentPointIndex = 1;

            pointSegmentTimer = 0f;


            int segments =
                pointPath.Count - 1;


            pointSegmentTime =
                time > 0f
                ? time / Mathf.Max(1, segments)
                : 0f;


            state =
                State.FollowingPoints;


            Debug.Log(
                "CameraPanEffect: No spline. Using POINT TO POINT."
            );
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
            playerPaused =
                true;
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
            state =
                State.Idle;

            ResumePlayer();

            return;
        }


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
            cam.position =
                returnPosition;

            cam.rotation =
                returnRotation;


            desiredPosition =
                returnPosition;

            desiredRotation =
                returnRotation;


            state =
                State.Idle;


            activeSpline = null;

            pointPath.Clear();

            panPoints.Clear();

            currentPointIndex = 0;

            pointInitialized = false;

            splineDistance = 0f;

            splineLength = 0f;

            pointSegmentTimer = 0f;

            pointSegmentTime = 0f;

            debugDistanceToTravel = 0f;

            debugDistanceTravelled = 0f;


            ResumePlayer();
        }
    }


    // =========================================================
    // DASH SKIP
    // =========================================================

    public void SkipPan()
    {
        // =====================================================
        // DO NOT SKIP PAUSE POINT MOVEMENT
        // =====================================================

        if (movingToPausePoint)
        {
            return;
        }


        // =====================================================
        // DO NOT SKIP PAUSE POINT RETURN
        // =====================================================

        if (returningFromPausePoint)
        {
            return;
        }


        // =====================================================
        // DO NOT SKIP WHILE WAITING AT PAUSE POINT
        // =====================================================

        if (atPausePoint)
        {
            return;
        }


        // =====================================================
        // NORMAL PAN
        // =====================================================

        if (state == State.Idle)
        {
            return;
        }


        // Mark door ready immediately.
        MarkDoorReady();


        Transform cam =
            CameraTransform;


        // Instantly return camera to
        // the position from before the pan.
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


        // Clear spline.
        activeSpline = null;


        // Clear points.
        pointPath.Clear();

        panPoints.Clear();


        // Reset point state.
        currentPointIndex = 0;

        pointInitialized = false;


        // Reset spline state.
        splineDistance = 0f;

        splineLength = 0f;


        // Reset timers.
        pointSegmentTimer = 0f;

        pointSegmentTime = 0f;


        // Reset debug.
        debugDistanceToTravel = 0f;

        debugDistanceTravelled = 0f;


        // Stop pan.
        state =
            State.Idle;


        // Give control back to player.
        ResumePlayer();
    }


    // =========================================================
    // DOOR
    // =========================================================

    private void MarkDoorReady()
    {
        if (doorReadyTriggered)
        {
            return;
        }


        if (doorSystem != null &&
            doorIndexToReady >= 0)
        {
            doorSystem.SetDoorReady(
                doorIndexToReady
            );

            doorIndexToReady = -1;
        }


        doorReadyTriggered =
            true;
    }


    // =========================================================
    // PLAYER
    // =========================================================

    private void ResumePlayer()
    {
        playerPaused =
            false;

        GlobalPanActive =
            false;
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
        // =====================================================
        // PAUSE POINT
        // =====================================================

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


        // =====================================================
        // START / END
        // =====================================================

        if (panStart_EndObjects != null)
        {
            Gizmos.color =
                Color.green;


            Gizmos.DrawSphere(
                panStart_EndObjects.position,
                0.5f
            );
        }


        // =====================================================
        // POINT PATH
        // =====================================================

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
            {
                continue;
            }


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