using System.Collections;
using UnityEngine;

/// <summary>
/// Applies burn damage over time and spawns burn VFX.
/// </summary>
public class BurnEffect : IStatusEffect
{
    private const float DAMAGE_TICK_INTERVAL = 1f;

    public float Duration
    {
        get { return 2f; }
    }

    private GameObject activeVfxInstance;

    public void Apply(StatusEffectContext context)
    {
        // Spawn VFX
        if (activeVfxInstance == null && BurnEffectAssets.BurnVfxPrefab != null)
        {
            activeVfxInstance = Object.Instantiate(BurnEffectAssets.BurnVfxPrefab, context.TargetTransform);
        }

        // Start damage routine
        context.TargetMono.StartCoroutine(DamageRoutine(context));
    }

    public void Remove(StatusEffectContext context)
    {
        if (activeVfxInstance != null)
        {
            Object.Destroy(activeVfxInstance);
            activeVfxInstance = null;
        }
    }

    private IEnumerator DamageRoutine(StatusEffectContext context)
    {
        float timer = Duration;

        while (timer > 0f)
        {
            timer -= DAMAGE_TICK_INTERVAL;

            Debug.Log($"{context.TargetTransform.name} takes burn damage.");

            yield return new WaitForSeconds(DAMAGE_TICK_INTERVAL);
        }
    }
}