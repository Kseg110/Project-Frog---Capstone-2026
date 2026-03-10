using UnityEngine;

/// <summary>
/// Provides effects with controlled access to the target entity.
/// </summary>
public struct StatusEffectContext
{
    public Transform TargetTransform { get; }
    public MonoBehaviour TargetMono { get; }

    public StatusEffectContext(Transform targetTransform, MonoBehaviour targetMono)
    {
        TargetTransform = targetTransform;
        TargetMono = targetMono;
    }
}