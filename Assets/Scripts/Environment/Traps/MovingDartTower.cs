using UnityEngine;

/// <summary>
/// Simple rotating behavior for the MovingDartTower trap.
/// Attach to the tower GameObject; adjust rotationAxis and rotationSpeed in the inspector.
/// </summary>
public class MovingDartTower : MonoBehaviour
{
    [Tooltip("Axis to rotate around (local space).")]
    public Vector3 rotationAxis = Vector3.up;

    [Tooltip("Rotation speed in degrees per second.")]
    public float rotationSpeed = 20f;

    // Update is called once per frame
    void Update()
    {
        if (rotationSpeed == 0f) return;
        transform.Rotate(rotationAxis.normalized * rotationSpeed * Time.deltaTime, Space.Self);
    }
}
