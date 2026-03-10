using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks active status effects and manages their durations.
/// </summary>
public class StatusEffectHandler : MonoBehaviour
{
    private Dictionary<Type, float> activeEffects = new Dictionary<Type, float>();
    private Dictionary<Type, IStatusEffect> effectInstances = new Dictionary<Type, IStatusEffect>();

    private StatusEffectContext context;

    private void Awake()
    {
        context = new StatusEffectContext(transform, this);
    }

    private void Update()
    {
        List<Type> expired = new List<Type>();

        foreach (KeyValuePair<Type, float> entry in activeEffects)
        {
            float remaining = entry.Value - Time.deltaTime;
            activeEffects[entry.Key] = remaining;

            if (remaining <= 0f)
            {
                expired.Add(entry.Key);
            }
        }

        for (int i = 0; i < expired.Count; i++)
        {
            Type type = expired[i];

            effectInstances[type].Remove(context);

            activeEffects.Remove(type);
            effectInstances.Remove(type);
        }
    }

    /// <summary>
    /// Applies or refreshes a status effect.
    /// </summary>
    public void ApplyEffect(IStatusEffect effect)
    {
        Type type = effect.GetType();

        if (activeEffects.ContainsKey(type))
        {
            activeEffects[type] = effect.Duration;
        }
        else
        {
            activeEffects.Add(type, effect.Duration);
            effectInstances.Add(type, effect);

            effect.Apply(context);
        }
    }
}
