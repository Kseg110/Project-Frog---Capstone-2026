using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class PlayerDashVFX : MonoBehaviour
{
    public static PlayerDashVFX Instance { get; private set; }
    private TrailRenderer trailRenderer;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"${gameObject.name} Instance already exists!", this.gameObject);
            return;
        }

        Instance = this;

        trailRenderer = GetComponent<TrailRenderer>();
    }

    private void Start()
    {
        trailRenderer.emitting = false;
    }

    public void StartDashVFX()
    {
        trailRenderer.emitting = true;
    }

    public void EndDashVFX()
    {
        trailRenderer.emitting = false;
    }
}
