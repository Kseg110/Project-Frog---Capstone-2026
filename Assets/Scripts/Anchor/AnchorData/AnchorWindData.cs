// WindAnchorData
// Shared ScriptableObject data asset for all wind-type anchors.
// Create one instance via Assets > Create > Anchor Data > Wind.
// Assign the same asset to every AnchorWind — upgrading it affects all of them.

using UnityEngine;

[CreateAssetMenu(fileName = "AnchorWindData", menuName = "Anchor Data/Wind")]
public class AnchorWindData : AnchorData
{
    [Header("Wind Settings")]
    [SerializeField] private float damageMultiplier = 0.7f; // 30% less damage (0.7x)

    public float DamageMultiplier
    {
        get => damageMultiplier;
        set => damageMultiplier = Mathf.Max(0f, value);
    }
}