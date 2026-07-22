// AnchorData
// Abstract base ScriptableObject for all anchor type data assets.
// Provides shared damage and tether range properties.
// Extended by FireAnchorData, IceAnchorData, and WindAnchorData.

using UnityEngine;

public abstract class AnchorData : ScriptableObject
{
    [Header("Base Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float tetherRange = 15f;

    public float Damage
    {
        get => damage;
        set => damage = Mathf.Max(0f, value);
    }

    public float TetherRange
    {
        get => tetherRange;
        set => tetherRange = Mathf.Max(0f, value);
    }
}