using UnityEngine;

public class FoliagePush : MonoBehaviour
{
    [Header("Bending Settings")]
    [SerializeField] private float bendAngle = 25f;
    [SerializeField] private float recoverSpeed = 5f;

    private Quaternion originalRotation;
    private Quaternion targetRotation;
    private Transform playerTransform;
    private bool isPlayerInside = false;

    private void Start()
    {
        originalRotation = transform.localRotation;
        targetRotation = originalRotation;
    }

    private void Update()
    {
        if (isPlayerInside && playerTransform != null)
        {
            // Calculate direction vector from plant to player
            Vector3 pushDirection = transform.position - playerTransform.position;
            pushDirection.y = 0f; // Keep rotation horizontal

            if (pushDirection.sqrMagnitude > 0.001f)
            {
                // Determine bend axis (perpendicular to push direction)
                Vector3 bendAxis = Vector3.Cross(Vector3.up, pushDirection.normalized);
                targetRotation = Quaternion.AngleAxis(bendAngle, bendAxis) * originalRotation;
            }
        }
        else
        {
            // Return to rest rotation when player leaves
            targetRotation = originalRotation;
        }

        // Smoothly interpolate towards target rotation
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * recoverSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }
}
