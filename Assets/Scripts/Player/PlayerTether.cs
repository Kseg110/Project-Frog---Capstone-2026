// TetherRenderer
// Renders a visual line between the player's fire point and the currently grappled tower
// using a LineRenderer. The line is only drawn while the player is actively grappling
// and a valid tower is attached. Automatically hides when the grapple is released.

using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TetherRenderer : MonoBehaviour
{
    [SerializeField] private PlayerGrapple playerGrapple;
    [SerializeField] private Transform firePoint;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
    }

    private void Update()
    {
        HandleTether();
    }

    private void HandleTether()
    {
        if (playerGrapple == null || firePoint == null)
            return;

        if (playerGrapple.IsGrappling && playerGrapple.CurrentTower != null)
        {
            DrawTether(playerGrapple.CurrentTower.transform.position);
        }
        else
        {
            _lineRenderer.enabled = false;
        }
    }

    private void DrawTether(Vector3 targetPosition)
    {
        _lineRenderer.enabled = true;

        _lineRenderer.SetPosition(0, firePoint.position);
        _lineRenderer.SetPosition(1, targetPosition);
    }
}