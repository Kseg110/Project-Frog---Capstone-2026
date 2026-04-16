using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class EnvFogDamage : MonoBehaviour
{
    [SerializeField] private float damagePerSecond = 1f;

    [Header("Tags That Can Be Damaged")]
    [SerializeField] private List<string> validTags = new List<string> { "Player" };

    private Dictionary<Health, float> damageTimers = new Dictionary<Health, float>();

    private void OnTriggerStay(Collider other)
    {
        // Get Health from parent (because collider might be on HitBox child)
        Health health = other.GetComponentInParent<Health>();

        if (health == null || health.IsDead)
            return;

        GameObject rootObject = health.gameObject;

        // Check if object's tag is allowed
        if (!validTags.Contains(rootObject.tag))
            return;

        if (!damageTimers.ContainsKey(health))
            damageTimers[health] = 0f;

        damageTimers[health] += Time.deltaTime;

        if (damageTimers[health] >= 1f)
        {
            health.TakeDmg(damagePerSecond);
            damageTimers[health] = 0f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Health health = other.GetComponentInParent<Health>();

        if (health != null && damageTimers.ContainsKey(health))
        {
            damageTimers.Remove(health);
        }
    }
}