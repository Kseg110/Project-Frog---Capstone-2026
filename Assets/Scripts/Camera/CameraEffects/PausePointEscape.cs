using UnityEngine;

public class PausePointEscape : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private CameraPanEffect cameraPanEffect;

    [SerializeField]
    private PauseManager pauseManager;

    private bool lastPausedState;


    private void Awake()
    {
        enabled = true;

        if (cameraPanEffect == null)
        {
            cameraPanEffect =
                FindAnyObjectByType<CameraPanEffect>();
        }

        if (pauseManager == null)
        {
            pauseManager =
                FindAnyObjectByType<PauseManager>();
        }

        if (pauseManager != null)
        {
            // KEEPING YOUR LOWERCASE ispaused
            lastPausedState =
                pauseManager.ispaused;
        }
    }


    private void OnDisable()
    {
        // If this component is disabled,
        // immediately enable it again.
        enabled = true;
    }


    private void Update()
    {
        if (cameraPanEffect == null ||
            pauseManager == null)
        {
            return;
        }


        // KEEPING YOUR LOWERCASE ispaused
        bool isPaused =
            pauseManager.ispaused;


        // =====================================================
        // SAME STATE = DO NOTHING
        // =====================================================

        if (isPaused == lastPausedState)
        {
            return;
        }


        // =====================================================
        // STATE CHANGED
        // =====================================================

        lastPausedState =
            isPaused;


        // =====================================================
        // NOT PAUSED -> PAUSED
        // =====================================================

        if (isPaused)
        {
            cameraPanEffect.MoveCameraToPausePoint();
        }


        // =====================================================
        // PAUSED -> NOT PAUSED
        // =====================================================

        else
        {
            cameraPanEffect.EndPausePoint();
        }
    }
}