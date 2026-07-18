using System.Collections.Generic;
using UnityEngine;

// Attach this to an empty parent GameObject & attach the helper (MudPitTrigger) to child collider(s).
// The Player (or any IMovement) slows while overlapping this mud pit's colliders.

public class MudPit : MonoBehaviour
{
    [Header("Slow strength")]
    [Tooltip("Multiplier applied to movement speed while inside. 0.5 = half speed, 0.75 = 25% slower. Must be > 0 (the speed system multiplies modifiers together).")]
    [Range(0f, 1f)]
    [SerializeField] private float speedMult = 0.5f;

    [Header("Which colliders count")]
    [Tooltip("Only colliders on these layers slow the victim. Exclude the tether hitbox layer so rope segments don't hold the slow on. Set to Everything to accept all.")]
    [SerializeField] private LayerMask affectedLayers = ~0;

    // For each victim, the set of THIS mud pit's colliders they currently overlap.
    private readonly Dictionary<IMovement, HashSet<Collider>> insideColliders = new Dictionary<IMovement, HashSet<Collider>>();

    public void HandleEnter(Collider other)
    {
        // Ignore colliders on non-affected layers (e.g. tether hitbox segments) entirely.
        if ((affectedLayers.value & (1 << other.gameObject.layer)) == 0) return;

        IMovement victim = other.GetComponentInParent<IMovement>();
        if (victim == null) return;

        if (!insideColliders.TryGetValue(victim, out HashSet<Collider> colliders))
        {
            colliders = new HashSet<Collider>();
            insideColliders[victim] = colliders;
        }

        // Add returns false if this exact collider was already tracked (duplicate enter) - ignore those.
        if (!colliders.Add(other)) return;

        // Apply the debuff only when the victim's FIRST counting collider enters.
        if (colliders.Count == 1)
            victim.AddSpeedModifier(this, speedMult);
    }

    public void HandleExit(Collider other)
    {
        if ((affectedLayers.value & (1 << other.gameObject.layer)) == 0) return;

        IMovement victim = other.GetComponentInParent<IMovement>();
        if (victim == null) return;

        if (!insideColliders.TryGetValue(victim, out HashSet<Collider> colliders))
            return;

        colliders.Remove(other);

        // Remove the debuff once the victim has no counting colliders left inside this mud pit.
        if (colliders.Count == 0)
        {
            victim.RemoveSpeedModifier(this);
            insideColliders.Remove(victim);
        }
    }

    private void OnDisable()
    {
        // If the mud pit is disabled while someone's inside, their exit events may never fire - clear proactively.
        foreach (var victim in insideColliders.Keys)
        {
            if (victim != null)
                victim.RemoveSpeedModifier(this);
        }
        insideColliders.Clear();
    }
}