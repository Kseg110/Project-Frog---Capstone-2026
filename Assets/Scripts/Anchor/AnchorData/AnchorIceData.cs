// IceAnchorData
// Shared ScriptableObject data asset for all ice-type anchors.
// Create one instance via Assets > Create > Anchor Data > Ice.
// Assign the same asset to every AnchorIce — upgrading it affects all of them.

using UnityEngine;

[CreateAssetMenu(fileName = "AnchorIceData", menuName = "Anchor Data/Ice")]
public class AnchorIceData : AnchorData
{
    [Header("Ice Settings")]
    [SerializeField] private float slowMultiplier = 0.5f;  // 50% movement speed
    [SerializeField] private float slowDuration = 2f;
    [SerializeField] private float damageMultiplier = 2f;  // x2 damage

    public float SlowMultiplier
    {
        get => slowMultiplier;
        set => slowMultiplier = Mathf.Clamp(value, 0f, 1f); // 0 = stop, 1 = no slow
    }

    public float SlowDuration
    {
        get => slowDuration;
        set => slowDuration = Mathf.Max(0f, value);
    }

    public float DamageMultiplier
    {
        get => damageMultiplier;
        set => damageMultiplier = Mathf.Max(0f, value);
    }
}