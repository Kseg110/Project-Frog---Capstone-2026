using UnityEngine;

/// <summary>
/// Freezes the target, preventing movement.
/// </summary>
public class FreezeEffect : IStatusEffect
{
    public float Duration
    {
        get { return 1.5f; }
    }

    private GameObject activeVfxInstance;

    public void Apply(StatusEffectContext context)
    {
        // TODO: Spawn freeze VFX
        // TODO: Disable movement
    }

    public void Remove(StatusEffectContext context)
    {
        // TODO: Destroy freeze VFX
        // TODO: Re-enable movement
    }
}