// FireAnchorData
// Shared ScriptableObject data asset for all fire-type anchors.
// Create one instance via Assets > Create > Anchor Data > Fire.
// Assign the same asset to every AnchorFire — upgrading it affects all of them.

using UnityEngine;

[CreateAssetMenu(fileName = "AnchorFireData", menuName = "Anchor Data/Fire")]
public class AnchorFireData : AnchorData
{
    [Header("Burn Settings")]
    [SerializeField] private float burnDuration = 2f;   // How long burn lasts
    [SerializeField] private float burnTickRate = 0.2f; // How often damage ticks

    public float BurnDuration
    {
        get => burnDuration;
        set => burnDuration = Mathf.Max(0f, value);
    }

    public float BurnTickRate
    {
        get => burnTickRate;
        set => burnTickRate = Mathf.Max(0.01f, value); // Prevent division by zero
    }
}