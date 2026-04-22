using UnityEngine;

/// <summary>
/// Provides static access to freeze-related assets.
/// </summary>
public static class FreezeEffectAssets
{
    public static GameObject FreezeVfxPrefab { get; private set; }

    public static void Initialize(GameObject vfxPrefab)
    {
        FreezeVfxPrefab = vfxPrefab;
    }
}
