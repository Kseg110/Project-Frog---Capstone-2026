using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class DamageParticleFog : MonoBehaviour
{
    public float damagePerSecond = 1f;

    private Dictionary<Health, float> damageTimers = new Dictionary<Health, float>();

    private void OnTriggerStay(Collider other)
    {
        // Only affect player
        if (!other.CompareTag("Player")) return;

        Health health = other.GetComponent<Health>();
        if (health == null || health.IsDead) return;

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
        if (!other.CompareTag("Player")) return;

        Health health = other.GetComponent<Health>();

        if (health != null && damageTimers.ContainsKey(health))
        {
            damageTimers.Remove(health);
        }
    }
}