using UnityEngine;

/// <summary>
/// Provides static access to slow-related assets.
/// </summary>
public static class SlowEffectAssets
{
    public static GameObject SlowVfxPrefab { get; private set; }

    public static void Initialize(GameObject vfxPrefab)
    {
        SlowVfxPrefab = vfxPrefab;
    }
}