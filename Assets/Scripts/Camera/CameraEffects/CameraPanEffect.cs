using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class CameraPanEffect : CameraEffectBase
{
    [Header("Pan Settings")]
    [SerializeField] private float panTime = 2f;
    [SerializeField] private float splineSpeed = 20f;

    [Header("Point To Point")]
    [SerializeField] private Transform panStartObject;
    [SerializeField] private Transform panEndObject;

    [SerializeField]
    private List<Transform> pointsOfInterest =
        new List<Transform>();

    [Header("Camera Startup")]
    [SerializeField] private float cameraStartupDelay = 1f;

    [Header("Spline")]
    [SerializeField] private CameraPanRoundTrigger roundTrigger;

    [Header("Door")]
    [SerializeField] private DoorSystem doorSystem;

    [Header("Player Control")]
    [SerializeField] private bool pausePlayerDuringPan = true;

    [SerializeField] private Vector3 debugPanStart;
    [SerializeField] private Vector3 debugPanEnd;

    private enum State
    {
        Idle,
        MovingToStart,
        FollowingSpline,
        Holding,
        Returning
    }

    private enum PanMode
    {
        Spline,
        PointToPoint
    }

    private State state =
        State.Idle;

    private PanMode panMode =
        PanMode.Spline;

    private CameraController controller;

    private Transform playerTransform;
    private PlayerMovement playerMovement;

    private bool playerPaused;

    private Vector3 desiredPosition;
    private Quaternion desiredRotation;

    private float timer;
    private float holdTimer;
    private float moveToStartTimer;

    private float currentHoldTime;

    private int doorIndexToReady = -1;
    private bool doorReadyTriggered;

    private bool allowSkipWithDash = true;

    //==========================
    // SPLINE
    //==========================

    private SplineContainer activeSpline;

    private float splineDistance;
    private float splineLength;

    //==========================
    // POINT TO POINT
    //==========================

    private readonly List<Vector3> pointPath =
        new List<Vector3>();

    private int currentPointIndex;

    private float pointSegmentTimer;
    private float pointSegmentTime;

    //==========================
    // CAMERA STARTUP
    //==========================

    private bool cameraReady;
    private float cameraReadyTimer;

    private void Awake()
    {
        controller =
            GetComponentInParent<CameraController>();

        playerTransform =
            GameObject.FindGameObjectWithTag("Player")
            ?.transform;

        if (playerTransform != null)
        {
            playerMovement =
                playerTransform.GetComponent<PlayerMovement>();
        }

        if (doorSystem == null)
        {
            doorSystem =
                FindAnyObjectByType<DoorSystem>();
        }

        if (roundTrigger == null)
        {
            roundTrigger =
                FindAnyObjectByType<CameraPanRoundTrigger>();
        }
    }

    private void Update()
    {
        if (panStartObject != null)
        {
            debugPanStart =
                panStartObject.position;
        }

        if (panEndObject != null)
        {
            debugPanEnd =
                panEndObject.position;
        }

        if (allowSkipWithDash &&
            IsPanning &&
            Input.GetButtonDown("Dash"))
        {
            SkipPan();
        }
    }

    //==========================================
    // POINT TO POINT MOVEMENT
    // Used ONLY when there is NO spline.
    //==========================================

    private void ApplyPointToPointMovement(float deltaTime)
    {
        if (pointPath.Count < 2)
        {
            state = State.Returning;
            return;
        }


        pointSegmentTimer += deltaTime;


        float t =
            Mathf.Clamp01(
                pointSegmentTimer /
                pointSegmentTime
            );


        t = EaseInOutQuad(t);


        // CURRENT SEGMENT
        Vector3 start =
            pointPath[currentPointIndex - 1];


        Vector3 end =
            pointPath[currentPointIndex];


        desiredPosition =
            Vector3.Lerp(
                start,
                end,
                t
            );


        Vector3 lookDir =
            end - desiredPosition;


        if (lookDir.sqrMagnitude > 0.001f)
        {
            desiredRotation =
                Quaternion.LookRotation(
                    lookDir.normalized,
                    Vector3.up
                );
        }


        // FINISHED THIS POINT
        if (t >= 1f)
        {
            desiredPosition = end;


            pointSegmentTimer = 0f;


            currentPointIndex++;


            // MORE POINTS
            if (currentPointIndex < pointPath.Count)
            {
                return;
            }


            // LAST POINT REACHED
            if (!doorReadyTriggered)
            {
                MarkDoorReady();
                doorReadyTriggered = true;
            }


            holdTimer = 0f;


            state = State.Holding;
        }
    }
    public override Vector3 ApplyEffect(float deltaTime)
    {
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

        if (state == State.Idle)
        {
            return Vector3.zero;
        }

        desiredPosition =
            transform.position;

        desiredRotation =
            transform.rotation;

        //==========================================
        // MOVE TO START
        //==========================================

        if (state == State.MovingToStart)
        {
            moveToStartTimer += deltaTime;

            float t =
                Mathf.Clamp01(
                    moveToStartTimer /
                    panTime
                );

            t =
                EaseInOutQuad(t);

            if (panMode == PanMode.Spline)
            {
                Vector3 firstPoint =
                    activeSpline.Spline
                    .EvaluatePosition(0f);

                desiredPosition =
                    Vector3.Lerp(
                        debugPanStart,
                        firstPoint,
                        t
                    );

                Vector3 tangent =
                    activeSpline.Spline
                    .EvaluateTangent(0f);

                if (tangent.sqrMagnitude > 0.001f)
                {
                    desiredRotation =
                        Quaternion.LookRotation(
                            tangent.normalized,
                            Vector3.up
                        );
                }

                if (moveToStartTimer >= panTime)
                {
                    desiredPosition =
                        firstPoint;

                    splineDistance = 0f;

                    state =
                        State.FollowingSpline;
                }
            }
            else
            {
                if (pointPath.Count < 2)
                {
                    state =
                        State.Returning;
                }
                else
                {
                    desiredPosition =
                        Vector3.Lerp(
                            pointPath[0],
                            pointPath[1],
                            t
                        );

                    Vector3 direction =
                        pointPath[1] -
                        pointPath[0];

                    if (direction.sqrMagnitude >
                        0.001f)
                    {
                        desiredRotation =
                            Quaternion.LookRotation(
                                direction.normalized,
                                Vector3.up
                            );
                    }

                    if (moveToStartTimer >=
                        panTime)
                    {
                        moveToStartTimer = 0f;

                        currentPointIndex = 0;

                        state =
                            State.FollowingSpline;
                    }
                }
            }
        }

        //==========================================
        // FOLLOW PATH
        //==========================================

        else if (state ==
            State.FollowingSpline)
        {
            if (panMode ==
                PanMode.Spline)
            {
                splineDistance +=
                    splineSpeed *
                    deltaTime;

                float progress =
                    Mathf.Clamp01(
                        splineDistance /
                        splineLength
                    );

                desiredPosition =
                    activeSpline.Spline
                    .EvaluatePosition(
                        progress
                    );

                Vector3 tangent =
                    activeSpline.Spline
                    .EvaluateTangent(
                        progress
                    );

                if (tangent.sqrMagnitude >
                    0.001f)
                {
                    desiredRotation =
                        Quaternion.LookRotation(
                            tangent.normalized,
                            Vector3.up
                        );
                }

                if (progress >= 1f)
                {
                    desiredPosition =
                        activeSpline.Spline
                        .EvaluatePosition(
                            1f
                        );

                    if (!doorReadyTriggered)
                    {
                        MarkDoorReady();

                        doorReadyTriggered =
                            true;
                    }

                    holdTimer = 0f;

                    state =
                        State.Holding;
                }
            }
            else
            {
                ApplyPointToPointMovement(
                    deltaTime
                );
            }
        }

        //==========================================
        // HOLD
        //==========================================

        else if (state ==
            State.Holding)
        {
            if (panMode ==
                PanMode.Spline)
            {
                desiredPosition =
                    activeSpline.Spline
                    .EvaluatePosition(
                        1f
                    );

                Vector3 tangent =
                    activeSpline.Spline
                    .EvaluateTangent(
                        1f
                    );

                if (tangent.sqrMagnitude >
                    0.001f)
                {
                    desiredRotation =
                        Quaternion.LookRotation(
                            tangent.normalized,
                            Vector3.up
                        );
                }
            }
            else
            {
                desiredPosition =
                    pointPath[
                        pointPath.Count - 1
                    ];

                if (pointPath.Count > 1)
                {
                    Vector3 direction =
                        pointPath[
                            pointPath.Count - 1
                        ] -
                        pointPath[
                            pointPath.Count - 2
                        ];

                    if (direction.sqrMagnitude >
                        0.001f)
                    {
                        desiredRotation =
                            Quaternion.LookRotation(
                                direction.normalized,
                                Vector3.up
                            );
                    }
                }
            }

            holdTimer += deltaTime;
            if (holdTimer >= currentHoldTime)
            {
                holdTimer = 0f;
                timer = 0f;

                state =
                    State.Returning;
            }
        }

        //==========================================
        // RETURN TO PLAYER
        //==========================================

        else if (state == State.Returning)
        {
            timer += deltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    panTime
                );


            t = EaseInOutQuad(t);


            Vector3 returnTarget;


            if (controller != null)
            {
                returnTarget =
                    controller.GetBasePosition();
            }
            else
            {
                returnTarget =
                    pointPath.Count > 0
                    ? pointPath[0]
                    : transform.position;
            }


            // Smooth return from current camera position
            desiredPosition =
                Vector3.Lerp(
                    transform.position,
                    returnTarget,
                    t
                );


            // Smoothly rotate back
            if (controller != null)
            {
                desiredRotation =
                    controller.transform.rotation;
            }


            if (timer >= panTime)
            {
                desiredPosition =
                    returnTarget;


                activeSpline = null;

                pointPath.Clear();


                currentPointIndex = 0;
                pointSegmentTimer = 0f;
                pointSegmentTime = 0f;


                timer = 0f;
                holdTimer = 0f;
                splineDistance = 0f;
                moveToStartTimer = 0f;


                state = State.Idle;


                if (playerPaused)
                {
                    ResumePlayer();
                }
            }
        }

        transform.position =
            desiredPosition;

        transform.rotation =
            desiredRotation;

        Vector3 effectOffset =
            controller != null
            ?
            desiredPosition -
            controller.GetBasePosition()
            :
            desiredPosition -
            transform.position;

        return effectOffset;
    }
    public void TriggerPan(
        List<CameraPanRoundTrigger.PanPoint> points,
        float time,
        int doorIndex,
        int round)
    {
        if (roundTrigger == null)
        {
            roundTrigger =
                FindAnyObjectByType<CameraPanRoundTrigger>();
        }


        activeSpline = null;


        if (roundTrigger != null)
        {
            activeSpline =
                roundTrigger.GetSplineForRound(round);
        }


        panTime = time;


        doorIndexToReady = doorIndex;
        doorReadyTriggered = false;


        pointPath.Clear();


        //==========================================
        // SPLINE MODE
        //==========================================

        if (activeSpline != null &&
            activeSpline.Spline != null &&
            activeSpline.Spline.Count >= 2)
        {
            panMode = PanMode.Spline;


            splineLength =
                activeSpline.Spline.GetLength();


            currentHoldTime =
                points[points.Count - 1].holdTime;
        }


        //==========================================
        // POINT TO POINT MODE
        //==========================================

        else
        {
            panMode = PanMode.PointToPoint;


            // FIRST POSITION IS THE REAL START
            // THIS IS ALSO THE RETURN LOCATION
            pointPath.Add(transform.position);


            if (points != null)
            {
                foreach (CameraPanRoundTrigger.PanPoint p in points)
                {
                    if (p != null &&
                        p.pointOfInterest != null)
                    {
                        Vector3 target =
                            p.pointOfInterest.position +
                            Vector3.up * 20f;


                        pointPath.Add(target);


                        currentHoldTime =
                            p.holdTime;
                    }
                }
            }


            if (pointPath.Count < 2)
            {
                Debug.LogWarning(
                    "CameraPanEffect: No Point To Point targets."
                );

                return;
            }


            currentPointIndex = 1;

            pointSegmentTimer = 0f;


            int segments =
                pointPath.Count - 1;


            pointSegmentTime =
                panTime /
                Mathf.Max(1, segments);
        }


        timer = 0f;
        holdTimer = 0f;
        moveToStartTimer = 0f;
        splineDistance = 0f;


        if (pausePlayerDuringPan &&
            !playerPaused)
        {
            if (playerMovement == null &&
                playerTransform != null)
            {
                playerMovement =
                    playerTransform.GetComponent<PlayerMovement>();
            }


            playerMovement?.StopMovement();


            playerPaused = true;
        }


        state = State.FollowingSpline;


        // ONLY SPLINE NEEDS MOVE TO START
        if (panMode == PanMode.Spline)
        {
            state = State.MovingToStart;
        }
    }
    public void SkipPan()
    {
        if (state == State.Idle)
        {
            return;
        }

        // Mark door as ready if not triggered yet
        if (!doorReadyTriggered)
        {
            MarkDoorReady();
            doorReadyTriggered = true;
        }

        Vector3 currentPlayerCameraPosition = transform.position;

        if (controller != null)
        {
            Vector3 controllerPosition = controller.GetBasePosition();

            if (controllerPosition != Vector3.zero)
            {
                currentPlayerCameraPosition = controllerPosition;
            }
        }

        transform.position = currentPlayerCameraPosition;

        activeSpline = null;

        pointPath.Clear();

        currentPointIndex = 0;
        pointSegmentTimer = 0f;
        pointSegmentTime = 0f;

        timer = 0f;
        holdTimer = 0f;
        splineDistance = 0f;
        moveToStartTimer = 0f;

        state = State.Idle;

        if (playerPaused)
        {
            ResumePlayer();
        }
    }

    private void MarkDoorReady()
    {
        if (doorSystem != null &&
            doorIndexToReady >= 0)
        {
            doorSystem.SetDoorReady(
                doorIndexToReady
            );

            doorIndexToReady = -1;
        }
    }

    private void ResumePlayer()
    {
        if (!playerPaused)
        {
            return;
        }

        if (playerMovement == null &&
            playerTransform != null)
        {
            playerMovement =
                playerTransform.GetComponent<PlayerMovement>();
        }

        playerMovement?.ResumeMovement();

        playerPaused = false;
    }

    private static float EaseInOutQuad(float t)
    {
        if (t < 0.5f)
        {
            return 2f * t * t;
        }

        return -1f +
            (4f - 2f * t) * t;
    }

    public bool IsPanning =>
        state != State.Idle;

    private void OnDrawGizmos()
    {
        if (panStartObject != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawSphere(
                panStartObject.position,
                0.5f
            );
        }

        if (panEndObject != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawSphere(
                panEndObject.position,
                0.5f
            );
        }

        if (panStartObject != null &&
            panEndObject != null)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawLine(
                panStartObject.position,
                panEndObject.position
            );
        }

        if (pointsOfInterest != null)
        {
            Gizmos.color = Color.cyan;

            foreach (Transform point in pointsOfInterest)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(
                        point.position,
                        0.3f
                    );
                }
            }
        }
    }
}