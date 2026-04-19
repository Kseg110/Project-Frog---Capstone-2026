using UnityEngine;

public class PlayerTongueAttack : MonoBehaviour
{
    [SerializeField] private Transform tongueMesh;

    [SerializeField] private float maxLength = 10f;
    [SerializeField] private float extendSpeed = 20f;
    [SerializeField] private float retractSpeed = 25f;
    [SerializeField] private float tongueWidth = 0.3f;

    private float currentLength = 0f;
    private bool extending = false; 
    private bool retracting = false; 

    public bool IsActive => extending || retracting;

    public System.Action OnTongueFinished;

    private void Awake()
    {
        if (tongueMesh == null)
        {
            Debug.LogError($"Please assign tongueMesh in ${gameObject.name}", this);
        }
    }

    /// <summary>
    /// Begins extending the tongue. Does nothing if already retracting.
    /// </summary>
    public void BeginTongueExtend()
    {
        if (retracting) return;
        extending = true;
        retracting = false;
    }

    /// <summary>
    /// Begins retracting the tongue. If already retracting, does nothing.
    /// Call this when letting go of Fire2, or hitting an enemy/fly to instantly begin retracting.
    /// </summary>
    public void BeginTongueRetract()
    {
        if (!retracting)
        {
            extending = false;
            retracting = true;
        }
    }

    private void Update()
    {
        if (extending)
        {
            currentLength += extendSpeed * Time.deltaTime;
            if (currentLength >= maxLength)
            {
                currentLength = maxLength;
                extending = false;
                BeginTongueRetract();
            }
        } 
        else if (retracting)
        {
            currentLength -= retractSpeed * Time.deltaTime;
            if (currentLength <= 0f)
            {
                currentLength = 0f;
                retracting = false;

                OnTongueFinished?.Invoke();
            }
        }

        UpdateTongueVisual();
    }

    private void UpdateTongueVisual()
    {
        tongueMesh.localScale = new Vector3(tongueWidth, currentLength / 2f, tongueWidth);
        tongueMesh.localPosition = new Vector3(0, 0, currentLength / 2f); //MAKE THIS 0, 0, 0 WHEN FINAL MESH IS ADDED, And make sure the pivot for that mesh isn't dead center.
    }
}

