using UnityEngine;

/// <summary>
/// Provides static access to burn-related assets.
/// </summary>
public static class BurnEffectAssets
{
    public static GameObject BurnVfxPrefab { get; private set; }

    public static void Initialize(GameObject vfxPrefab)
    {
        BurnVfxPrefab = vfxPrefab;
    }
}
