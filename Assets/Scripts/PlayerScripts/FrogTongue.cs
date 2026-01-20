using UnityEngine;

public class FrogTongue : MonoBehaviour
{
    [SerializeField] private Transform tongueMesh;
    [SerializeField] private float maxLength = 10f;
    [SerializeField] private float extendSpeed = 20f;
    [SerializeField] private float retractSpeed = 25f;
    [SerializeField] private float tongueWidth = 0.3f;

    private float currentLength = 0f;
    private bool extending = false;
    public bool retracting = false;

    public void BeginTongue()
    {
        extending = true;
        retracting = false;
        currentLength = 0f;
        UpdateTongueVisual();
    }

    public void EndTongue()
    {
        extending = false;
        retracting = true;
        currentLength = 0f;
        UpdateTongueVisual();
    }

    private void Update()
    {
        if (!extending) return;

        if (extending)
        {
            currentLength += extendSpeed * Time.deltaTime;
            if (currentLength >= maxLength)
            {
                currentLength = maxLength;
                extending = false;
            }
        } 
        else if (retracting)
        {
            currentLength -= retractSpeed * Time.deltaTime;
            if (currentLength <= 0f)
            {
                currentLength = 0f;
                retracting = false;
            }
        }

        UpdateTongueVisual();
    }

    private void UpdateTongueVisual()
    {
        tongueMesh.localScale = new Vector3(tongueWidth, currentLength / 2f, tongueWidth);
        tongueMesh.localPosition = new Vector3(0, 0, currentLength / 2f);
    }
}

