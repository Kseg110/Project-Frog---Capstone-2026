using UnityEngine;

[RequireComponent(typeof(Collider))]
/// <summary>
/// Area trigger that applies a poison status effect to entities entering or staying inside it.
/// </summary>
public class PoisonCloud : MonoBehaviour
{
    private IStatusEffect poisonEffect = new PoisonEffect();

    private void Awake()
    {
        Collider cloudCollider = GetComponent<Collider>();

        if (cloudCollider == null)
        {
            Debug.LogError($"Class {nameof(PoisonCloud)} requires a Collider component.");
            return;
        }

        if (!cloudCollider.isTrigger)
        {
            Debug.LogError($"Class {nameof(PoisonCloud)} requires the Collider to be set as a trigger.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyPoison(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryApplyPoison(other);
    }

    /// <summary>
    /// Attempts to apply the poison effect to the target if it has a StatusEffectHandler.
    /// </summary>
    private void TryApplyPoison(Collider other)
    {
        if (other == null)
        {
            return;
        }

        StatusEffectHandler statusEffectHandler = other.GetComponent<StatusEffectHandler>();

        if (statusEffectHandler != null)
        {
            statusEffectHandler.ApplyEffect(poisonEffect);
        }
    }
}
