using UnityEngine;

/// <summary>
/// Interface for all status effects.
/// </summary>
public interface IStatusEffect
{
    float Duration { get; }

    void Apply(StatusEffectContext context);
    void Remove(StatusEffectContext context);
}
