using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class OverchargeSlowEffect : MonoBehaviour
{
    private float slowPercent;
    private float duration;
    private Coroutine slowCoroutine;
    private bool isSlowed = false;

    private NavMeshAgent navAgent;
    private float originalSpeed;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            Debug.LogWarning($"[SlowEffect] No EnemyBase component found on {gameObject.name}");
        }
    }

    public void ApplySlow(float percent, float totalDuration)
    {
        slowPercent = percent;
        duration = totalDuration;

        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
            RemoveSlow();
        }
        slowCoroutine = StartCoroutine(SlowCoroutine());
    }

    private IEnumerator SlowCoroutine()
    {
        if (navAgent != null)
        {
            originalSpeed = navAgent.speed;
            float slowMultiplier = 1f - (slowPercent / 100f);
            navAgent.speed = originalSpeed * slowMultiplier;
            isSlowed = true;
        }
        yield return new WaitForSeconds(duration);

        RemoveSlow();
        Destroy(this);
    }

    private void RemoveSlow()
    {
        if (isSlowed && navAgent != null)
        {
            navAgent.speed = originalSpeed;
            isSlowed = false;
        }
    }

    private void OnDestroy()
    {
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }
        RemoveSlow();
    }
}
