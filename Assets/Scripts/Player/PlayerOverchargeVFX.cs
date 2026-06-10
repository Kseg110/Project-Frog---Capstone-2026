using UnityEngine;

[RequireComponent (typeof(TrailRenderer))]
public class PlayerOverchargeVFX : MonoBehaviour
{
    [Header("Trail Colors")]
    [SerializeField] private Color fireColor = Color.red;
    [SerializeField] private Color iceColor = Color.cyan;
    [SerializeField] private Color windColor = Color.green;
    [SerializeField] private Color defaultColor = Color.white;

    [Header("Trail Settings")]
    [SerializeField] private float trailTime = 0.5f;
    [SerializeField] private AnimationCurve trailWidthCurve = AnimationCurve.Linear(0, 1, 1, 0);
    [SerializeField] private float trailWidth = 1f;

    private TrailRenderer trailRenderer;
    private Material trailMaterial;

    private void Awake()
    {
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer.material != null)
        {
            trailMaterial = new Material(trailRenderer.material);
            trailRenderer.material = trailMaterial;
        }
    }

    void Start()
    {
        trailRenderer.emitting = false;
        trailRenderer.time = trailTime;
        trailRenderer.widthCurve = trailWidthCurve;
        trailRenderer.widthMultiplier = trailWidth;
    }

    public void StartOverchargeTrail(AnchorBase anchor)
    {
        trailRenderer.emitting = true;
        SetTrailColor(anchor);
    }

    public void EndOverchargeTrail()
    {
        trailRenderer.emitting = false;
    }

    private void SetTrailColor(AnchorBase anchor)
    {
        if (anchor == null)
        {
            SetColor(defaultColor);
            return;
        }

        // Get the runtime type
        System.Type anchorType = anchor.GetType();
        string typeName = anchorType.Name;
        
        //Debug.Log($"[PlayerOverchargeVFX] Setting trail color for anchor type: {typeName}");

        // Check specific anchor for types
        if (anchor is AnchorFire)
        {
            SetColor(fireColor);
        }
        else if (anchor is AnchorIce)
        {
            SetColor(iceColor);
        }
        else if (anchor is AnchorWind)
        {
            SetColor(windColor);
        }
        else
        {
            SetColor(defaultColor);
        }
    }

    private void SetColor(Color color)
    {
        if (trailRenderer != null)
        {
            // Set gradient with chosen color from anchor
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color, 1f)
                 },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trailRenderer.colorGradient = gradient;
        }
        else
        {
            Debug.LogError("[PlayerOverchargeVFX] TrailRenderer is null!");
        }
    }

    public void UpdateTrailColor(AnchorBase anchor)
    {
        SetTrailColor(anchor);
    }

    private void OnDestroy()
    {
        // material instance cleanup
        if (trailMaterial != null)
        {
            Destroy(trailMaterial);
        }
    }

}
