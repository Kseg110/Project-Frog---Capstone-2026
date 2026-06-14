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

    public virtual void Activate()
    {
        RuntimeManager.PlayOneShot(BaseData.ActivationEvent, transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, TetherRange);
    }
}