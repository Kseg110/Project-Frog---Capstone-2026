using System.Collections;
using UnityEngine;

public class PoisonEffect : IStatusEffect
{
    private const float DAMAGE_TICK_INTERVAL = 2f;
    private const float DAMAGE_AMOUNT = 20f;

    public float Duration => 2f;

    private GameObject activeVfxInstance;

    public void Apply(StatusEffectContext context)
    {
        if (activeVfxInstance == null && PoisonEffectAssets.PoisonVfxPrefab != null)
        {
            activeVfxInstance = Object.Instantiate(
                PoisonEffectAssets.PoisonVfxPrefab,
                context.TargetTransform
            );
        }

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

            if (context.TargetDamageable != null)
            {
                context.TargetDamageable.TakeDmg(DAMAGE_AMOUNT);
            }

            yield return new WaitForSeconds(DAMAGE_TICK_INTERVAL);
        }
    }
}
