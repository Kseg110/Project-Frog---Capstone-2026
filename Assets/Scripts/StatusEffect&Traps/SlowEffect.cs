using UnityEngine;

/// <summary>
/// Slows the target's movement speed.
/// </summary>
public class SlowEffect : IStatusEffect
{
    public float Duration
    {
        get { return 2f; }
    }

    private GameObject activeVfxInstance;

    public void Apply(StatusEffectContext context)
    {
        // TODO: Spawn slow VFX
        // TODO: Reduce movement speed
    }

    public void Remove(StatusEffectContext context)
    {
        // TODO: Destroy slow VFX
        // TODO: Restore movement speed
    }
}