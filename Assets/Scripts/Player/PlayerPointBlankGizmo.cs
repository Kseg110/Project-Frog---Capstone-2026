using UnityEngine;

public class PointBlankGizmo : MonoBehaviour
{
    public float radius = 10f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}