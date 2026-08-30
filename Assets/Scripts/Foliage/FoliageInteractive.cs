using UnityEngine;

public class FoliageInteractive : MonoBehaviour
{
    [Header("Ambient Wind Sway")]
    [SerializeField] private float swaySpeed = 1.8f;
    [SerializeField] private float swayAngle = 3.5f;
    [SerializeField] private bool randomizeOffset = true;

    [Header("Player Contact Response")]
    [SerializeField] private float bendAngle = 25f;
    [SerializeField] private float recoverSpeed = 5f;

    private Quaternion baseRotation;
    private Quaternion targetBendRotation;
    private Transform playerTransform;
    private bool isPlayerInside = false;
    private float timeOffset;

    private void Start()
    {
        baseRotation = transform.localRotation;
        timeOffset = randomizeOffset ? Random.Range(0f, 100f) : 0f;
        targetBendRotation = Quaternion.identity;
    }

    private void Update()
    {
        // 1. Calculate Ambient Wind Offset
        float zSway = Mathf.Sin((Time.time + timeOffset) * swaySpeed) * swayAngle;
        float xSway = Mathf.Cos((Time.time + timeOffset) * (swaySpeed * 0.7f)) * (swayAngle * 0.5f);
        Quaternion windRotation = baseRotation * Quaternion.Euler(xSway, 0f, zSway);

        // 2. Handle Player Displacement
        if (isPlayerInside && playerTransform != null)
        {
            Vector3 pushDir = transform.position - playerTransform.position;
            pushDir.y = 0f;

            if (pushDir.sqrMagnitude > 0.001f)
            {
                Vector3 bendAxis = Vector3.Cross(Vector3.up, pushDir.normalized);
                targetBendRotation = Quaternion.AngleAxis(bendAngle, bendAxis);
            }
        }
        else
        {
            targetBendRotation = Quaternion.identity;
        }

        // 3. Blend Wind + Bend & Apply
        Quaternion finalTarget = windRotation * targetBendRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, finalTarget, Time.deltaTime * recoverSpeed);
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