using UnityEngine;
using System.Collections;

public class OverchargeBurnEffect : MonoBehaviour
{
    private Health health;
    private Coroutine burnCoroutine;
    private float damagePerTick;
    private float tickInterval;
    private float duration;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (health == null)
        {
            Debug.LogWarning($"[BurnEffect] No Health component found on {gameObject.name}.");
        }
    }

    public void ApplyBurn(float damage, float interval, float totalDuration)
    {
        damagePerTick = damage;
        tickInterval = interval;
        duration = totalDuration;

        // stops exisitng burn effect - restart effect
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
        }
        burnCoroutine = StartCoroutine(BurnCoroutine());
    }

    private IEnumerator BurnCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (health != null && !health.IsDead)
            {
                health.TakeDmg(damagePerTick);
            }
            else
            {
                break;
            }
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }
        Destroy(this);
    }

    private void OnDestroy()
    {
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
        }
    }
}
