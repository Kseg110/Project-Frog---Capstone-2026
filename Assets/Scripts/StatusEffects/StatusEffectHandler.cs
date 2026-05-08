using System;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectHandler : MonoBehaviour
{
    private Dictionary<Type, float> activeEffects = new Dictionary<Type, float>();
    private Dictionary<Type, IStatusEffect> effectInstances = new Dictionary<Type, IStatusEffect>();

    private StatusEffectContext context;

    private void Awake()
    {
        IDamageable damageable = GetComponent<IDamageable>();

        context = new StatusEffectContext(
            transform,
            this,
            damageable
        );
    }

    private void Update()
    {
        List<Type> expired = new List<Type>();

        foreach (var entry in activeEffects)
        {
            float remaining = entry.Value - Time.deltaTime;
            activeEffects[entry.Key] = remaining;

            if (remaining <= 0f)
                expired.Add(entry.Key);
        }

        foreach (Type type in expired)
        {
            effectInstances[type].Remove(context);
            activeEffects.Remove(type);
            effectInstances.Remove(type);
        }
    }

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
