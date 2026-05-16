using UnityEngine;

/// <summary>
/// Provides static access to poison-related assets.
/// </summary>
public static class PoisonEffectAssets
{
    public static GameObject PoisonVfxPrefab { get; private set; }

    public static void Initialize(GameObject vfxPrefab)
    {
        PoisonVfxPrefab = vfxPrefab;
    }
}
