using UnityEngine;

public class HealthbarAlignmentScript : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;

    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;

        transform.rotation = Quaternion.identity;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
