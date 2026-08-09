using FMODUnity;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

public class CameraPanEffect : CameraEffectBase
{

    //==================================================
    // PAN SETTINGS
    //==================================================

    [Header("Pan Settings")]

    [Tooltip("Controls how fast the camera moves along the spline path.")]
    [SerializeField]
    private float splineSpeed = 20f;
    // New: global flag other systems (PlayerMovement) can query to know a camera pan is active.
    public static bool GlobalPanActive = false;

    [SerializeField]
    private bool GlobalPanActiveInspector;
    //==================================================
    // POINT TO POINT SETTINGS
    //==================================================

    [Header("Point To Point")]

    [Tooltip("Starting camera position used when no spline exists.")]
    [SerializeField]
    private Transform panStart_EndObjects;


    //==================================================
    // CAMERA STARTUP
    //==================================================

    [Header("Camera Startup")]

    [Tooltip("Delay before the camera pan system becomes active.")]
    [SerializeField]
    private float cameraStartupDelay = 1f;


    //==================================================
    // SPLINE REFERENCES
    //==================================================

    [Header("Spline")]

    [Tooltip("Reference used to find the spline assigned to the current round.")]
    [SerializeField]
    private CameraPanRoundTrigger roundTrigger;


    //==================================================
    // DOOR SYSTEM
    //==================================================

    [Header("Door")]

    [Tooltip("Door system used to mark the door as ready after the camera pan finishes.")]
    [SerializeField]
    private DoorSystem doorSystem;


    //==================================================
    // PLAYER CONTROL
    //==================================================

    [Header("Player Control")]

    [Tooltip("Stops player movement while the camera pan is active.")]
    [SerializeField]
    private bool pausePlayerDuringPan = true;
    //==================================================
    // CAMERA STATE TYPES
    //==================================================

    [Header("Camera State Types")]

    [Tooltip("Current stage of the camera pan.")]
    [SerializeField]
    private State state = State.Idle;


    [Tooltip("Current movement system being used.")]
    [SerializeField]
    private PanMode panMode = PanMode.Spline;



    //==================================================
    // CAMERA REFERENCES
    //==================================================

    [Header("Camera References")]

    [Tooltip("Reference to the main camera controller.")]
    [SerializeField]
    private CameraController controller;


    [Tooltip("Player transform used for returning camera control.")]
    [SerializeField]
    private Transform playerTransform;






    //==================================================
    // PLAYER PAUSE STATE
    //==================================================

    [Header("Player Pause State")]

    [Tooltip("Tracks if player movement is currently disabled.")]
    [SerializeField]
    private bool playerPaused;



    //==================================================
    // CAMERA TARGET VALUES
    //==================================================

    [Header("Camera Target Values")]

    [Tooltip("Current target position calculated by the camera effect.")]
    [SerializeField]
    private Vector3 desiredPosition;


    [Tooltip("Current target rotation calculated by the camera effect.")]
    [SerializeField]
    private Quaternion desiredRotation;



    //==================================================
    // DOOR TRACKING
    //==================================================

    [Header("Door Tracking")]

    [Tooltip("Door index that will become ready when the pan finishes.")]
    [SerializeField]
    private int doorIndexToReady = -1;


    [Tooltip("Prevents the door ready event from being called multiple times.")]
    [SerializeField]
    private bool doorReadyTriggered;



    //==================================================
    // PAN INPUT
    //==================================================

    [Header("Pan Input")]

    [Tooltip("Allows the player to skip the camera pan using dash.")]
    [SerializeField]
    private bool allowSkipWithDash = true;



    //==================================================
    // SPLINE RUNTIME DATA
    //==================================================

    [Header("Spline Runtime Data")]

    [Tooltip("Spline currently being followed by the camera.")]
    [SerializeField]
    private SplineContainer activeSpline;


    [Tooltip("Current distance travelled along the spline.")]
    [SerializeField]
    private float splineDistance;


    [Tooltip("Total distance length of the active spline.")]
    [SerializeField]
    private float splineLength;



    //==================================================
    // POINT TO POINT RUNTIME DATA
    //==================================================






    [SerializeField]
    private List<Vector3> pointPath =
        new List<Vector3>();




    [Tooltip("Current point index the camera is moving toward.")]
    [SerializeField]
    private int currentPointIndex;


    [Tooltip("Timer tracking movement between two points.")]
    [SerializeField]
    private float pointSegmentTimer;


    [Tooltip("Time required to travel between two points.")]
    [SerializeField]
    private float pointSegmentTime;



    //==================================================
    // SPLINE START MOVEMENT DEBUG
    //==================================================

    [Header("Spline Start Movement Debug")]

    [Tooltip("Distance threshold for reaching the spline starting knot.")]
    [SerializeField]
    private float splineKnotReachDistance = 1f;


    [Tooltip("Speed used when moving the camera to the spline start.")]
    [SerializeField]
    private float moveToStartSpeed = 0.5f;


    [Tooltip("Distance the camera needs to travel to reach spline start.")]
    [SerializeField]
    private float debugDistanceToTravel;


    [Tooltip("Distance already travelled toward spline start.")]
    [SerializeField]
    private float debugDistanceTravelled;



    //==================================================
    // CAMERA READY STATE
    //==================================================

    [Header("Camera Ready State")]

    [Tooltip("True when the camera startup delay has finished.")]
    [SerializeField]
    private bool cameraReady;


    [Tooltip("Current timer value before the camera starts.")]
    [SerializeField]
    private float cameraReadyTimer;
    //==================================================
    // CAMERA STATE TYPES
    //==================================================

    private enum State
    {
        Idle,
        MovingToStart,
        FollowingSpline,
        Holding,
        Returning
    }


    //==================================================
    // CAMERA MOVEMENT TYPE
    //==================================================

    private enum PanMode
    {
        Spline,
        PointToPoint
    }
    private void Awake()
    {
        controller =
            GetComponentInParent<CameraController>();

        playerTransform =
            GameObject.FindGameObjectWithTag("Player")
            ?.transform;



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
        GlobalPanActiveInspector = GlobalPanActive;



        if (allowSkipWithDash &&
            IsPanning &&
            Input.GetButtonDown("Dash"))
        {
            SkipPan();
        }
    }

    private void ApplyPointToPointMovement(float deltaTime)
    {
        // No valid path, return camera
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



        // CAMERA LOOK DIRECTION
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



        // FINISHED CURRENT POINT
        if (t >= 1f)
        {
            desiredPosition = end;


            pointSegmentTimer = 0f;


            currentPointIndex++;



            // MORE POINTS LEFT
            if (currentPointIndex < pointPath.Count)
            {
                return;
            }



            // FINAL POINT REACHED
            if (!doorReadyTriggered)
            {
                MarkDoorReady();
                doorReadyTriggered = true;
            }



            // START RETURNING TO PLAYER CAMERA
            state = State.Returning;
        }
    }
    //==========================
    // SPLINE KNOT TRACKING
    //==========================



    //==========================================
    // APPLY EFFECT
    //==========================================
    public override Vector3 ApplyEffect(float deltaTime)

    {
        if (!cameraReady)
        {
            cameraReadyTimer += deltaTime;

            if (cameraReadyTimer < cameraStartupDelay)
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
        // MOVE TO START OF SPLINE
        //==========================================

        if (state == State.MovingToStart)
        {
            if (activeSpline == null)
            {
                state = State.Returning;
            }
            else
            {
                Vector3 startKnotPosition =
                    activeSpline.Spline.EvaluatePosition(0f);


                debugDistanceToTravel =
                    Vector3.Distance(
                        panStart_EndObjects.position,
                        startKnotPosition
                    );


                debugDistanceTravelled +=
                    moveToStartSpeed * deltaTime;


                float travelled =
                    Mathf.Min(
                        debugDistanceTravelled,
                        debugDistanceToTravel
                    );


                float t =
                    debugDistanceToTravel <= 0.001f
                    ? 1f
                    :
                    travelled /
                    debugDistanceToTravel;


                desiredPosition =
                    Vector3.Lerp(
                        panStart_EndObjects.position,
                        startKnotPosition,
                        t
                    );


                Vector3 direction =
                    startKnotPosition -
                    desiredPosition;


                if (direction.sqrMagnitude > 0.001f)
                {
                    desiredRotation =
                        Quaternion.LookRotation(
                            direction.normalized,
                            Vector3.up
                        );
                }


                if (travelled >= debugDistanceToTravel)
                {
                    desiredPosition =
                        startKnotPosition;


                    splineDistance = 0f;


                    debugDistanceTravelled =
                        debugDistanceToTravel;


                    state =
                        State.FollowingSpline;
                }
            }
        }



        //==========================================
        // FOLLOW SPLINE
        //==========================================

        else if (state == State.FollowingSpline)
        {
            if (panMode == PanMode.Spline)
            {
                if (activeSpline == null ||
                    activeSpline.Spline == null)
                {
                    state =
                        State.Returning;

                    return Vector3.zero;
                }



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



                Vector3 splineTangent =
                    activeSpline.Spline
                    .EvaluateTangent(
                        progress
                    );


                if (splineTangent.sqrMagnitude > 0.001f)
                {
                    desiredRotation =
                        Quaternion.LookRotation(
                            splineTangent.normalized,
                            Vector3.up
                        );
                }



                // END OF SPLINE

                if (progress >= 1f)
                {
                    if (!doorReadyTriggered)
                    {
                        MarkDoorReady();
                        doorReadyTriggered = true;
                    }


                    state =
                        State.Returning;
                }
            }
            else
            {
                ApplyPointToPointMovement(deltaTime);
            }
        }



        //==========================================
        // RETURN
        //==========================================

        else if (state == State.Returning)
        {
            Vector3 returnTarget;


            if (controller != null)
            {
                returnTarget =
                    controller.GetBasePosition();
            }
            else
            {
                returnTarget =
                    transform.position;
            }



            Vector3 direction =
                returnTarget -
                transform.position;



            float distance =
                direction.magnitude;



            if (distance <= 0.01f)
            {
                desiredPosition =
                    returnTarget;


                state =
                    State.Idle;


                activeSpline = null;


                pointPath.Clear();


                currentPointIndex = 0;


                splineDistance = 0f;



                if (playerPaused)
                {
                    ResumePlayer();
                }
            }
            else
            {
                desiredPosition =
                    transform.position +
                    direction.normalized;
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
            Vector3.zero;



        return effectOffset;
    }
    private List<CameraPanRoundTrigger.PanPoint> activePanPoints;


    [System.Serializable]
    public class PanPoint
    {
        public Vector3 pointPosition;
        public float holdTime = 5f;
    }


    [SerializeField]
    private List<PanPoint> panPoints =
        new List<PanPoint>();



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


        doorIndexToReady = doorIndex;
        doorReadyTriggered = false;



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
        }



        //==========================================
        // POINT TO POINT MODE
        //==========================================

        else
        {
            panMode = PanMode.PointToPoint;


            activePanPoints = points;


            pointPath.Add(
                transform.position
            );



            if (activePanPoints != null)
            {
                for (int i = 0; i < activePanPoints.Count; i++)
                {
                    CameraPanRoundTrigger.PanPoint panPoint =
                        activePanPoints[i];


                    if (panPoint == null)
                        continue;


                    if (panPoint.pointOfInterest == null)
                        continue;



                    Vector3 target =
                        panPoint.pointOfInterest.position +
                        Vector3.up * 20f;



                    // CAMERA MOVEMENT PATH
                    pointPath.Add(target);


                    PanPoint newPoint =
                        new PanPoint();


                    newPoint.pointPosition =
                        target;


                    newPoint.holdTime =
                        panPoint.holdTime;


                    panPoints.Add(newPoint);



                    Debug.Log(
                        "Pan Point " + i +
                        " Position: " + target +
                        " Hold Time: " + panPoint.holdTime
                    );
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
                5f /
                Mathf.Max(1, segments);
        }



        splineDistance = 0f;



        if (pausePlayerDuringPan &&
            !playerPaused)
        {

            GlobalPanActive = true;


            playerPaused = true;
        }



        if (panMode == PanMode.Spline)
        {
            state = State.MovingToStart;
        }
        else
        {
            state = State.FollowingSpline;
        }
    }
    //==========================================
    // BUILD POINT PATH FROM POINT OF INTEREST LIST
    // Takes PanPoint list from CameraPanRoundTrigger
    // Stores locations AND hold times by index.
    //==========================================
    //private void BuildPointPathFromPoints(
    //    List<CameraPanRoundTrigger.PanPoint> points)
    //{
    //    pointPath.Clear();

    //    pointHoldTimes.Clear();


    //    // Camera starting position
    //    pointPath.Add(
    //        transform.position
    //    );


    //    // Start point has no hold time
    //    pointHoldTimes.Add(0f);



    //    if (points == null ||
    //        points.Count == 0)
    //    {
    //        Debug.LogWarning(
    //            "CameraPanEffect: No PanPoints found."
    //        );

    //        return;
    //    }



    //    foreach (CameraPanRoundTrigger.PanPoint point in points)
    //    {
    //        if (point == null)
    //            continue;


    //        if (point.pointOfInterest == null)
    //            continue;



    //        Vector3 location =
    //            point.pointOfInterest.position +
    //            Vector3.up * 20f;



    //        // Add location
    //        pointPath.Add(location);



    //        // Add matching hold time
    //        pointHoldTimes.Add(
    //            point.holdTime
    //        );
    //    }



    //    currentPointIndex = 1;

    //    pointSegmentTimer = 0f;


    //    int segments =
    //        pointPath.Count - 1;


    //    pointSegmentTime =
    //        5f /
    //        Mathf.Max(
    //            1,
    //            segments
    //        );



    //    // Debug positions and holds
    //    for (int i = 0; i < pointPath.Count; i++)
    //    {
    //        Debug.Log(
    //            "Camera Point [" +
    //            i +
    //            "] Location: " +
    //            pointPath[i] +
    //            " Hold Time: " +
    //            pointHoldTimes[i]
    //        );
    //    }
    //}
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

        splineDistance = 0f;

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


        GlobalPanActive = false;

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
        if (panStart_EndObjects != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawSphere(
                 panStart_EndObjects.position,
                0.5f
            );
        }

        if (panStart_EndObjects != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawSphere(
                 panStart_EndObjects.position,
                0.5f
            );
        }

        if (panStart_EndObjects != null &&
             panStart_EndObjects != null)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawLine(
                 panStart_EndObjects.position,
                 panStart_EndObjects.position
            );
        }

    }
}