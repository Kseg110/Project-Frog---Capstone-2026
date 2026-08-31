using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class CameraPanRoundTrigger : MonoBehaviour
{
    // ============================================================
    // PAN POINT
    // ============================================================

    [System.Serializable]
    public class PanPoint
    {
        public Transform pointOfInterest;

        [Header("Hold")]
        [Tooltip("How long the camera holds at this point.")]
        public float holdTime = 5f;
    }


    // ============================================================
    // ROUND PAN
    // ============================================================

    [System.Serializable]
    public class RoundPan
    {
        [Header("Return")]

        [Tooltip(
            "ON = return to the original camera position and " +
            "rotation in exactly Return Time seconds."
        )]
        public bool fixedReturnTime = true;

        [Tooltip(
            "Exact return duration when Fixed Return Time is ON."
        )]
        public float returnTime = 1f;


        [Header("Round")]

        public int round;


        [Header("Pan Points")]

        public List<PanPoint> panPoints =
            new List<PanPoint>();


        [Header("Pan")]

        public float panTime = 2f;


        [Header("Door")]

        public int doorIndex = 0;
    }


    // ============================================================
    // SPLINE SYNC DIRECTION
    // ============================================================
    //
    // TRUE:
    //     SPLINE -> GAME OBJECT
    //
    //     Position + Rotation
    //
    //
    // FALSE:
    //     GAME OBJECT -> SPLINE
    //
    //     Position + Rotation
    //
    // ============================================================

    [Header("Spline Sync")]

    [SerializeField]
    private bool syncSplineToPanPoints = true;


    // ============================================================
    // SYNC INTERVAL
    // ============================================================

    [Header("Sync")]

    [Tooltip(
        "How often the spline/object synchronization is checked."
    )]
    [SerializeField]
    private float syncInterval = 60f;


    private double nextSyncTime;


    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]

    [SerializeField]
    private CameraPanEffect cameraPan;

    [SerializeField]
    private WaveRoundSystem waveSystem;


    // ============================================================
    // ROUND
    // ============================================================

    [Header("Round")]

    [SerializeField]
    private int currentRound = 1;


    // ============================================================
    // ROUND PATHS
    // ============================================================

    [Header("Round Paths")]

    [SerializeField]
    private List<RoundPan> roundPans =
        new List<RoundPan>();


    // ============================================================
    // SPLINE SETTINGS
    // ============================================================

    [Header("Spline Settings")]

    [Tooltip(
        "The spline is this amount above the matching game object."
    )]
    [SerializeField]
    private float heightOffset = 20f;


    // ============================================================
    // ROUND ACTIVATION TRACKING
    // ============================================================
    //
    // The same round can NEVER trigger repeatedly while the
    // player remains on that round.
    //
    // If the player goes to another round and later comes back,
    // that round is allowed to activate again.
    //
    // Example:
    //
    // 1 -> activate
    // 1 -> nothing
    // 1 -> nothing
    // 2 -> activate
    // 2 -> nothing
    // 1 -> activate again
    // 1 -> nothing
    //
    // ============================================================

    private int lastActivatedRound =
        int.MinValue;


    // ============================================================
    // SPLINE COUNT TRACKING
    // ============================================================

    private Dictionary<int, int> knownSplineCounts =
        new Dictionary<int, int>();


    // ============================================================
    // ENABLE
    // ============================================================

    private void OnEnable()
    {
        ScheduleNextSync();
    }


    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        // ========================================================
        // UPDATE CURRENT ROUND
        // ========================================================

        if (Application.isPlaying &&
            waveSystem != null)
        {
            currentRound =
                waveSystem.CurrentWave;
        }


        // ========================================================
        // TIMED SPLINE / OBJECT SYNC
        // ========================================================
        //
        // This does NOT run every frame.
        //
        // It checks once every syncInterval seconds.
        //
        // ========================================================

        if (GetCurrentTime() >= nextSyncTime)
        {
            ScheduleNextSync();

            SyncAllSplinesAndPanPoints();
        }


        // ========================================================
        // PLAY MODE ONLY
        // ========================================================

        if (!Application.isPlaying)
            return;


        if (cameraPan == null)
            return;


        // ========================================================
        // SAME ROUND = NEVER ACTIVATE AGAIN
        // ========================================================
        //
        // Staying on the same round does NOT restart the pan.
        //
        // ========================================================

        if (currentRound == lastActivatedRound)
            return;


        // ========================================================
        // WAIT UNTIL CURRENT PAN IS FINISHED
        // ========================================================

        if (cameraPan.IsPanning)
            return;


        // ========================================================
        // FIND CURRENT ROUND
        // ========================================================

        foreach (RoundPan pan in roundPans)
        {
            if (pan == null)
                continue;


            if (pan.round != currentRound)
                continue;


            if (pan.panPoints == null ||
                pan.panPoints.Count == 0)
            {
                continue;
            }


            // ====================================================
            // MARK ROUND AS ACTIVATED
            // ====================================================
            //
            // Mark BEFORE TriggerPan so Update cannot trigger it
            // again while remaining on this round.
            //
            // ====================================================

            lastActivatedRound =
                currentRound;


            // ====================================================
            // ACTIVATE CAMERA PAN
            // ====================================================

            cameraPan.TriggerPan(
                pan.panPoints,
                pan.panTime,
                pan.doorIndex,
                pan.round,
                pan.fixedReturnTime
            );


            break;
        }
    }


    // ============================================================
    // CURRENT TIME
    // ============================================================

    private double GetCurrentTime()
    {
#if UNITY_EDITOR

        if (!Application.isPlaying)
        {
            return EditorApplication.timeSinceStartup;
        }

#endif

        return Time.realtimeSinceStartup;
    }


    // ============================================================
    // SCHEDULE NEXT SYNC
    // ============================================================

    private void ScheduleNextSync()
    {
        float interval =
            Mathf.Max(
                0.1f,
                syncInterval
            );


        nextSyncTime =
            GetCurrentTime() +
            interval;
    }


    // ============================================================
    // GET FIXED RETURN TIME
    // ============================================================

    public bool GetFixedReturnTime()
    {
        foreach (RoundPan pan in roundPans)
        {
            if (pan == null)
                continue;


            if (pan.round != currentRound)
                continue;


            return pan.fixedReturnTime;
        }


        return false;
    }


    // ============================================================
    // GET RETURN TIME
    // ============================================================

    public float GetReturnTime()
    {
        foreach (RoundPan pan in roundPans)
        {
            if (pan == null)
                continue;


            if (pan.round != currentRound)
                continue;


            return Mathf.Max(
                0.01f,
                pan.returnTime
            );
        }


        return 1f;
    }


    // ============================================================
    // GET PAN POINTS
    // ============================================================

    public List<PanPoint> GetPanPoints()
    {
        foreach (RoundPan pan in roundPans)
        {
            if (pan == null)
                continue;


            if (pan.round != currentRound)
                continue;


            return pan.panPoints;
        }


        return null;
    }


    // ============================================================
    // GET ROUND PANS
    // ============================================================

    public List<RoundPan> GetRoundPans()
    {
        return roundPans;
    }


    // ============================================================
    // MASTER SYNC
    // ============================================================

    private void SyncAllSplinesAndPanPoints()
    {
        if (roundPans == null)
            return;


        bool changedAnything =
            false;


        // ========================================================
        // PROCESS EVERY ROUND
        // ========================================================

        foreach (RoundPan roundPan in roundPans)
        {
            if (roundPan == null)
                continue;


            if (roundPan.panPoints == null)
            {
                roundPan.panPoints =
                    new List<PanPoint>();
            }


            SplineContainer splineContainer =
                GetSplineForRound(
                    roundPan.round
                );


            if (splineContainer == null)
                continue;


            if (splineContainer.Spline == null)
                continue;


            int splineCount =
                splineContainer.Spline.Count;


            // ====================================================
            // DETECT NEW SPLINE POINTS
            // ====================================================

            int previousSplineCount;

            bool alreadyKnown =
                knownSplineCounts.TryGetValue(
                    roundPan.round,
                    out previousSplineCount
                );


            if (!alreadyKnown)
            {
                knownSplineCounts[
                    roundPan.round
                ] = splineCount;

                previousSplineCount =
                    splineCount;
            }


            // ====================================================
            // ONLY CREATE OBJECTS FOR NEW SPLINE POINTS
            // ====================================================

            if (splineCount > previousSplineCount)
            {
                for (
                    int i = previousSplineCount;
                    i < splineCount;
                    i++
                )
                {
                    if (i >= roundPan.panPoints.Count)
                    {
                        CreatePanPointFromSpline(
                            roundPan,
                            splineContainer,
                            i
                        );

                        changedAnything =
                            true;
                    }
                }


                knownSplineCounts[
                    roundPan.round
                ] = splineCount;
            }


            // ====================================================
            // SAFETY IF PAN POINT LIST IS SHORTER
            // ====================================================

            while (
                roundPan.panPoints.Count <
                splineCount
            )
            {
                int index =
                    roundPan.panPoints.Count;


                CreatePanPointFromSpline(
                    roundPan,
                    splineContainer,
                    index
                );


                changedAnything =
                    true;
            }


            // ====================================================
            // MATCH SPLINE POINTS TO GAME OBJECTS
            // ====================================================

            int count =
                Mathf.Min(
                    roundPan.panPoints.Count,
                    splineCount
                );


            for (int i = 0; i < count; i++)
            {
                PanPoint panPoint =
                    roundPan.panPoints[i];


                if (panPoint == null)
                    continue;


                if (panPoint.pointOfInterest == null)
                    continue;


                Transform gameObject =
                    panPoint.pointOfInterest;


                BezierKnot knot =
                    splineContainer.Spline[i];


                // =================================================
                // SPLINE -> GAME OBJECT
                // =================================================

                if (syncSplineToPanPoints)
                {
                    if (SyncSplineToGameObject(
                        splineContainer,
                        knot,
                        gameObject
                    ))
                    {
                        changedAnything =
                            true;
                    }
                }


                // =================================================
                // GAME OBJECT -> SPLINE
                // =================================================

                else
                {
                    if (SyncGameObjectToSpline(
                        splineContainer,
                        knot,
                        gameObject,
                        i
                    ))
                    {
                        changedAnything =
                            true;
                    }
                }
            }
        }


#if UNITY_EDITOR

        if (changedAnything)
        {
            EditorUtility.SetDirty(this);

            SceneView.RepaintAll();
        }

#endif
    }


    // ============================================================
    // SPLINE -> GAME OBJECT
    // ============================================================

    private bool SyncSplineToGameObject(
        SplineContainer splineContainer,
        BezierKnot knot,
        Transform gameObject
    )
    {
        // ========================================================
        // POSITION
        // ========================================================

        Vector3 splineWorldPosition =
            splineContainer.transform.TransformPoint(
                knot.Position
            );


        Vector3 desiredPosition =
            splineWorldPosition -
            Vector3.up * heightOffset;


        // ========================================================
        // ROTATION
        // ========================================================

        Quaternion desiredRotation =
            splineContainer.transform.rotation *
            knot.Rotation;


        // ========================================================
        // CHECK POSITION
        // ========================================================

        bool positionDifferent =
            Vector3.Distance(
                gameObject.position,
                desiredPosition
            ) > 0.001f;


        // ========================================================
        // CHECK ROTATION
        // ========================================================

        bool rotationDifferent =
            Quaternion.Angle(
                gameObject.rotation,
                desiredRotation
            ) > 0.1f;


        // ========================================================
        // NOTHING TO CHANGE
        // ========================================================

        if (!positionDifferent &&
            !rotationDifferent)
        {
            return false;
        }


#if UNITY_EDITOR

        if (!Application.isPlaying)
        {
            Undo.RecordObject(
                gameObject,
                "Sync Spline To Game Object"
            );
        }

#endif


        // ========================================================
        // ONLY CHANGE POSITION IF NEEDED
        // ========================================================

        if (positionDifferent)
        {
            gameObject.position =
                desiredPosition;
        }


        // ========================================================
        // ONLY CHANGE ROTATION IF NEEDED
        // ========================================================

        if (rotationDifferent)
        {
            gameObject.rotation =
                desiredRotation;
        }


#if UNITY_EDITOR

        EditorUtility.SetDirty(
            gameObject
        );

#endif

        return true;
    }


    // ============================================================
    // GAME OBJECT -> SPLINE
    // ============================================================

    private bool SyncGameObjectToSpline(
        SplineContainer splineContainer,
        BezierKnot knot,
        Transform gameObject,
        int index
    )
    {
        // ========================================================
        // POSITION
        // ========================================================

        Vector3 desiredWorldPosition =
            gameObject.position +
            Vector3.up * heightOffset;


        Vector3 desiredLocalPosition =
            splineContainer.transform
                .InverseTransformPoint(
                    desiredWorldPosition
                );


        // ========================================================
        // ROTATION
        // ========================================================

        Quaternion desiredLocalRotation =
            Quaternion.Inverse(
                splineContainer.transform.rotation
            ) *
            gameObject.rotation;


        // ========================================================
        // CHECK POSITION
        // ========================================================

        bool positionDifferent =
            Vector3.Distance(
                knot.Position,
                desiredLocalPosition
            ) > 0.001f;


        // ========================================================
        // CHECK ROTATION
        // ========================================================

        bool rotationDifferent =
            Quaternion.Angle(
                knot.Rotation,
                desiredLocalRotation
            ) > 0.1f;


        // ========================================================
        // NOTHING TO CHANGE
        // ========================================================

        if (!positionDifferent &&
            !rotationDifferent)
        {
            return false;
        }


#if UNITY_EDITOR

        if (!Application.isPlaying)
        {
            Undo.RecordObject(
                splineContainer,
                "Sync Game Object To Spline"
            );
        }

#endif


        // ========================================================
        // ONLY CHANGE POSITION IF NEEDED
        // ========================================================

        if (positionDifferent)
        {
            knot.Position =
                desiredLocalPosition;
        }


        // ========================================================
        // ONLY CHANGE ROTATION IF NEEDED
        // ========================================================

        if (rotationDifferent)
        {
            knot.Rotation =
                desiredLocalRotation;
        }


        // ========================================================
        // WRITE KNOT
        // ========================================================

        splineContainer.Spline[index] =
            knot;


#if UNITY_EDITOR

        EditorUtility.SetDirty(
            splineContainer
        );

#endif

        return true;
    }


    // ============================================================
    // CREATE PAN POINT FROM NEW SPLINE POINT
    // ============================================================

    private void CreatePanPointFromSpline(
        RoundPan roundPan,
        SplineContainer splineContainer,
        int index
    )
    {
        if (splineContainer == null)
            return;


        if (splineContainer.Spline == null)
            return;


        if (index < 0 ||
            index >= splineContainer.Spline.Count)
        {
            return;
        }


        // ========================================================
        // GET KNOT
        // ========================================================

        BezierKnot knot =
            splineContainer.Spline[index];


        // ========================================================
        // SPLINE WORLD POSITION
        // ========================================================

        Vector3 splineWorldPosition =
            splineContainer.transform.TransformPoint(
                knot.Position
            );


        // ========================================================
        // OBJECT POSITION
        // ========================================================

        Vector3 objectPosition =
            splineWorldPosition -
            Vector3.up * heightOffset;


        // ========================================================
        // OBJECT ROTATION
        // ========================================================

        Quaternion objectRotation =
            splineContainer.transform.rotation *
            knot.Rotation;


        // ========================================================
        // CREATE OBJECT
        // ========================================================

        GameObject newObject =
            new GameObject(
                "CameraPanPoint_" +
                roundPan.round +
                "_" +
                index
            );


        newObject.transform.position =
            objectPosition;


        newObject.transform.rotation =
            objectRotation;


        // ========================================================
        // CREATE PAN POINT
        // ========================================================

        PanPoint newPanPoint =
            new PanPoint();


        newPanPoint.pointOfInterest =
            newObject.transform;


        newPanPoint.holdTime =
            5f;


        roundPan.panPoints.Add(
            newPanPoint
        );


#if UNITY_EDITOR

        Undo.RegisterCreatedObjectUndo(
            newObject,
            "Create Camera Pan Point"
        );


        EditorUtility.SetDirty(
            this
        );

#endif
    }


    // ============================================================
    // GET SPLINE FOR ROUND
    // ============================================================

    public SplineContainer GetSplineForRound(
        int round
    )
    {
        Transform splineObject =
            transform.Find(
                "CameraPanSpline_Round_" +
                round
            );


        if (splineObject == null)
            return null;


        return splineObject.GetComponent<
            SplineContainer
        >();
    }


    // ============================================================
    // MANUAL SYNC
    // ============================================================

    public void SyncPanPointsWithSpline(
        int round,
        bool splineToPanPoints
    )
    {
        RoundPan roundPan = null;


        foreach (RoundPan pan in roundPans)
        {
            if (pan == null)
                continue;


            if (pan.round == round)
            {
                roundPan = pan;
                break;
            }
        }


        if (roundPan == null)
            return;


        SplineContainer splineContainer =
            GetSplineForRound(round);


        if (splineContainer == null)
            return;


        if (splineContainer.Spline == null)
            return;


        int count =
            Mathf.Min(
                roundPan.panPoints.Count,
                splineContainer.Spline.Count
            );


        for (int i = 0; i < count; i++)
        {
            PanPoint panPoint =
                roundPan.panPoints[i];


            if (panPoint == null ||
                panPoint.pointOfInterest == null)
            {
                continue;
            }


            Transform gameObject =
                panPoint.pointOfInterest;


            BezierKnot knot =
                splineContainer.Spline[i];


            if (splineToPanPoints)
            {
                SyncSplineToGameObject(
                    splineContainer,
                    knot,
                    gameObject
                );
            }
            else
            {
                SyncGameObjectToSpline(
                    splineContainer,
                    knot,
                    gameObject,
                    i
                );
            }
        }


#if UNITY_EDITOR

        EditorUtility.SetDirty(
            splineContainer
        );

        EditorUtility.SetDirty(
            this
        );

        SceneView.RepaintAll();

#endif
    }


    // ============================================================
    // SET ROUND
    // ============================================================

    public void SetRound(int round)
    {
        // ========================================================
        // ONLY CHANGE THE ROUND
        // ========================================================

        if (currentRound == round)
            return;


        currentRound =
            round;


        // ========================================================
        // IMPORTANT:
        //
        // Do NOT set lastActivatedRound here.
        //
        // That allows the new/current round to activate its pan.
        //
        // Going:
        //
        // 1 -> 2
        // 2 -> 1
        //
        // allows both rounds to activate again.
        //
        // ========================================================
    }


    // ============================================================
    // BUILD ALL SPLINES
    // ============================================================

    public void BuildAllSplines()
    {
        ClearOldSplines();

        knownSplineCounts.Clear();


        foreach (RoundPan round in roundPans)
        {
            if (round == null)
                continue;


            if (round.panPoints == null ||
                round.panPoints.Count == 0)
            {
                continue;
            }


            CreateSpline(round);
        }


#if UNITY_EDITOR

        EditorUtility.SetDirty(
            this
        );

        SceneView.RepaintAll();

#endif
    }


    // ============================================================
    // CLEAR OLD SPLINES
    // ============================================================

    private void ClearOldSplines()
    {
        for (
            int i = transform.childCount - 1;
            i >= 0;
            i--
        )
        {
            Transform child =
                transform.GetChild(i);


            if (!child.name.StartsWith(
                "CameraPanSpline_Round_"
            ))
            {
                continue;
            }


#if UNITY_EDITOR

            DestroyImmediate(
                child.gameObject
            );

#else

            Destroy(
                child.gameObject
            );

#endif
        }
    }


    // ============================================================
    // CREATE SPLINE
    // ============================================================

    private void CreateSpline(
        RoundPan round
    )
    {
        GameObject splineObject =
            new GameObject(
                "CameraPanSpline_Round_" +
                round.round
            );


        splineObject.transform.SetParent(
            transform
        );


        splineObject.transform.localPosition =
            Vector3.zero;


        splineObject.transform.localRotation =
            Quaternion.identity;


        splineObject.transform.localScale =
            Vector3.one;


        SplineContainer container =
            splineObject.AddComponent<
                SplineContainer
            >();


        Spline spline =
            new Spline();


        // ========================================================
        // PAN POINT -> SPLINE
        // ========================================================

        foreach (PanPoint point in round.panPoints)
        {
            if (point == null ||
                point.pointOfInterest == null)
            {
                continue;
            }


            // ====================================================
            // SPLINE POSITION
            // ====================================================
            //
            // X = game object X
            // Y = game object Y + heightOffset
            // Z = game object Z
            //
            // ====================================================

            Vector3 worldPosition =
                point.pointOfInterest.position +
                Vector3.up * heightOffset;


            Vector3 localPosition =
                container.transform
                    .InverseTransformPoint(
                        worldPosition
                    );


            // ====================================================
            // SPLINE ROTATION
            // ====================================================

            Quaternion localRotation =
                Quaternion.Inverse(
                    container.transform.rotation
                ) *
                point.pointOfInterest.rotation;


            // ====================================================
            // ADD KNOT
            // ====================================================

            spline.Add(
                new BezierKnot(
                    localPosition,
                    Vector3.zero,
                    Vector3.zero,
                    localRotation
                )
            );
        }


        container.Spline =
            spline;


#if UNITY_EDITOR

        EditorUtility.SetDirty(
            container
        );

#endif
    }
}