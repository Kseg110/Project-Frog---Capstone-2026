using System.Collections.Generic;
using UnityEngine;

public class CameraPanRoundTrigger : MonoBehaviour
{
    [System.Serializable]
    public class PanPoint
    {
        public Transform pointOfInterest;

        // This point's own hold time
        public float holdTime = 5f;
    }

    [System.Serializable]
    public class RoundPan
    {
        public int round;

        // List of points for this round
        public List<PanPoint> panPoints = new List<PanPoint>();

        // Move time between points
        public float panTime = 2f;

        // Door index that becomes ready after LAST point
        public int doorIndex = 0;
    }


    [Header("References")]
    [SerializeField] private CameraPanEffect cameraPan;

    [SerializeField] private WaveRoundSystem waveSystem;


    [Header("Round")]
    [SerializeField] private int currentRound = 1;


    [Header("Round Triggers")]
    [SerializeField] private List<RoundPan> roundPans = new List<RoundPan>();


    private int previousRound = int.MinValue;


    private void Update()
    {
        if (waveSystem != null)
            currentRound = waveSystem.CurrentWave;


        if (cameraPan == null)
            return;


        // Do not start another pan while one is active
        if (cameraPan.IsPanning)
            return;


        // Only trigger when round changes
        if (currentRound == previousRound)
            return;


        previousRound = currentRound;


        foreach (RoundPan pan in roundPans)
        {
            if (pan.round == currentRound &&
                pan.panPoints != null &&
                pan.panPoints.Count > 0)
            {
                Debug.Log(
                    "Camera Pan Round "
                    + currentRound
                    + " Door "
                    + pan.doorIndex
                );


                cameraPan.TriggerPan(
                    pan.panPoints,
                    pan.panTime,
                    pan.doorIndex
                );


                break;
            }
        }
    }


    public void SetRound(int round)
    {
        currentRound = round;
    }
}