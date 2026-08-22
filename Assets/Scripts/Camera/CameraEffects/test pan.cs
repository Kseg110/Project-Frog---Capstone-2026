using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor;
#endif


[ExecuteAlways]
public class CameraPanRoundTrigger : MonoBehaviour
{
    [System.Serializable]
    public class PanPoint
    {
        public Transform pointOfInterest;

        [Header("Hold")]
        [Tooltip("How long the camera holds at this point.")]
        public float holdTime = 5f;



        [Tooltip("Time used when Fixed Return Time is ON.")]
        public float returnTime = 1f;
    }


    [System.Serializable]
    public class RoundPan
    {
        public int round;
        [Header("Return")]
        [Tooltip("ON = return to the original camera position in exactly returnTime seconds. OFF = return using normal pan speed.")]
        public bool fixedReturnTime = false;
        public List<PanPoint> panPoints =
            new List<PanPoint>();

        public float panTime = 2f;

        public int doorIndex = 0;
    }
    public bool GetFixedReturnTime()
    {
        foreach (RoundPan pan in roundPans)
        {
            if (pan != null &&
                pan.round == currentRound)
            {
                return pan.fixedReturnTime;
            }
        }

        return false;
    }
    public List<PanPoint> GetPanPoints()
    {
        foreach (RoundPan pan in roundPans)
        {
            if (pan != null &&
                pan.round == currentRound)
            {
                return pan.panPoints;
            }
        }

        return null;
    }


    [Header("References")]
    [SerializeField] private CameraPanEffect cameraPan;
    [SerializeField] private WaveRoundSystem waveSystem;



    [Header("Round")]
    [SerializeField] private int currentRound = 1;



    [Header("Round Paths")]
    [SerializeField]
    private List<RoundPan> roundPans =
        new List<RoundPan>();



    [Header("Spline Settings")]
    [SerializeField]
    private float heightOffset = 20f;



    private int previousRound =
        int.MinValue;



    private void Update()
    {
        if (!Application.isPlaying)
            return;



        if (waveSystem != null)
        {
            currentRound =
                waveSystem.CurrentWave;
        }



        if (cameraPan == null)
            return;



        if (cameraPan.IsPanning)
            return;



        if (currentRound == previousRound)
            return;



        previousRound =
            currentRound;



        foreach (RoundPan pan in roundPans)
        {
            if (pan == null)
                continue;



            if (pan.round == currentRound &&
                pan.panPoints != null &&
                pan.panPoints.Count > 0)
            {
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
    }
    public SplineContainer GetSplineForRound(int round)
    {
        Transform splineObject =
            transform.Find(
                "CameraPanSpline_Round_" + round
            );


        if (splineObject == null)
        {
            Debug.LogWarning(
                "No spline found for round " + round
            );

            return null;
        }


        return splineObject.GetComponent<SplineContainer>();
    }



    public List<RoundPan> GetRoundPans()
    {
        return roundPans;
    }



    public void SetRound(int round)
    {
        currentRound = round;
    }
    public void BuildAllSplines()
    {
        ClearOldSplines();

        foreach (RoundPan round in roundPans)
        {
            if (round == null)
                continue;

            if (round.panPoints == null ||
                round.panPoints.Count == 0)
                continue;

            CreateSpline(round);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }



    private void ClearOldSplines()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child =
                transform.GetChild(i);

            if (child.name.StartsWith(
                "CameraPanSpline_Round_"))
            {
#if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
            }
        }
    }



    private void CreateSpline(RoundPan round)
    {
        GameObject splineObject =
            new GameObject(
                "CameraPanSpline_Round_" + round.round
            );


        splineObject.transform.SetParent(transform);


        SplineContainer container =
            splineObject.AddComponent<SplineContainer>();


        Spline spline =
            new Spline();


        foreach (PanPoint point in round.panPoints)
        {
            if (point == null)
                continue;

            if (point.pointOfInterest == null)
                continue;


            Vector3 position =
                point.pointOfInterest.position +
                Vector3.up * heightOffset;


            spline.Add(
                new BezierKnot(position)
            );
        }


        container.Spline =
            spline;


#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(container);
#endif
    }
}