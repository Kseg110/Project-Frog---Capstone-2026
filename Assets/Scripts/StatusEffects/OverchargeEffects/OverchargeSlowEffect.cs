using System.Collections;
using UnityEngine;

public class OverchargeSlowEffect : MonoBehaviour
{
    private float slowPercent;
    private float duration;
    private Coroutine slowCoroutine;

    private IMovement movement;

    private void Awake()
    {
        movement = GetComponentInParent<IMovement>();
        if (movement == null)
            Debug.LogWarning($"[SlowEffect] No IMovement found on {gameObject.name}");
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
        if (movement != null)
        {
            // Convert percent (50)  multiplier (0.5). 
            float slowMultiplier = 1f - (slowPercent / 100f);
            movement.AddSpeedModifier(this, slowMultiplier);
        }

        yield return new WaitForSeconds(duration);

        RemoveSlow();
        Destroy(this);
    }

    private void RemoveSlow()
    {
        if (movement != null)
            movement.RemoveSpeedModifier(this);
    }

    private void OnDestroy()
    {
        if (slowCoroutine != null)
            StopCoroutine(slowCoroutine);

        RemoveSlow();
    }
}