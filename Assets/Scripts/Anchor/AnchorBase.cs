// AnchorBase
// Abstract base component for all anchor types (Fire, Ice, Wind).
// Reads shared stats from an AnchorData ScriptableObject asset.
// Extended by AnchorFire, AnchorIce, and AnchorWind.

using UnityEngine;
using FMODUnity;

public abstract class AnchorBase : MonoBehaviour
{
    public abstract AnchorData BaseData { get; }
    public abstract AnchorElement Element { get; }

    public float Damage => BaseData != null ? BaseData.Damage : 0f;
    public float TetherRange => BaseData != null ? BaseData.TetherRange : 0f;

    [Header("Overcharge Point Light")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float baseIntensity = 1f;
    [SerializeField] private float maxOverchargeIntensity = 50f;

    private float originalIntensity;
    private float targetIntensity;

    protected virtual void Awake()
    {
        if (pointLight == null)
        {
            pointLight = GetComponentInChildren<Light>();
        }
        if (pointLight != null)
        {
            originalIntensity = pointLight.intensity;
            baseIntensity = originalIntensity;
            targetIntensity = originalIntensity;
        }
    }

    private void Update()
    {
        if (pointLight != null)
        {
            pointLight.intensity = Mathf.Lerp(
                pointLight.intensity,
                targetIntensity,
                Time.deltaTime * 5f
                );
        }
    }

    public virtual void Activate()
    {
        RuntimeManager.PlayOneShot(BaseData.ActivationEvent, transform.position);
    }

    public void UpdateOverchargeVisual(float normalizedProgress)
    {
        if (pointLight != null)
        {
            targetIntensity = Mathf.Lerp(baseIntensity, maxOverchargeIntensity, normalizedProgress);
        }
    }

    public void ResetOverchargeVisual()
    {
        if (pointLight != null)
        {
            targetIntensity = originalIntensity;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, TetherRange);
    }
}