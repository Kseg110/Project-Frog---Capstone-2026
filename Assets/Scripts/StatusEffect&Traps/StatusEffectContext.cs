using UnityEngine;

/// <summary>
/// Provides effects with controlled access to the target entity.
/// </summary>
public struct StatusEffectContext
{
    public Transform TargetTransform { get; }
    public MonoBehaviour TargetMono { get; }
    public IDamageable TargetDamageable { get; }

    public StatusEffectContext(Transform targetTransform, MonoBehaviour targetMono, IDamageable targetDamageable)
    {
        TargetTransform = targetTransform;
        TargetMono = targetMono;
        TargetDamageable = targetDamageable;
    }
}