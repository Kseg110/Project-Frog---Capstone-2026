using System.Collections.Generic;
using UnityEngine;

// Attach this to an empty parent GameObject & attach the helper (MudPitTrigger) to child collider(s).
// The Player (or any IMovement) slows while overlapping this mud pit's colliders.
public class MudPit : MonoBehaviour
{
    [Header("Slow strength")]
    [SerializeField] private float speedMult = 0.5f;

    [Header("Filtering")]
    [Tooltip("Only colliders on these layers count. Exclude transient/spawned collider layers.")]
    [SerializeField] private LayerMask affectedLayers = ~0;

    [Header("Spawn Overlap Check")]
    [Tooltip("Colliders on the trigger children used to detect already-overlapping victims at start.")]
    [SerializeField] private Collider[] triggerColliders;

    // For each victim, the set of THIS mud pit's colliders they currently overlap.
    private readonly Dictionary<IMovement, HashSet<Collider>> insideColliders = new Dictionary<IMovement, HashSet<Collider>>();

    private void Start()
    {
        // Catch anything already standing inside the pit when it (or the enemy) spawns.
        if (triggerColliders == null || triggerColliders.Length == 0) return;
        foreach (var trig in triggerColliders)
        {
            if (trig == null) continue;
            Collider[] overlaps = Physics.OverlapBox(
                trig.bounds.center,
                trig.bounds.extents,
                trig.transform.rotation,
                affectedLayers,
                QueryTriggerInteraction.Ignore
            );
            foreach (var hit in overlaps)
                HandleEnter(hit);
        }
    }

    public void HandleEnter(Collider other)
    {
        if ((affectedLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        // Skip colliders explicitly opted out (tether hitboxes, overcharge trail colliders).
        // They're children of the Player but aren't the Player's movement body, and they get disabled/destroyed mid-overlap without firing OnTriggerExit - which would otherwise strand a slow modifier and permanently slow the player.
        if (other.GetComponentInParent<MudPitIgnore>() != null)
            return;

        IMovement victim = other.GetComponentInParent<IMovement>();
        if (victim == null)
            return;

        if (!insideColliders.TryGetValue(victim, out var set))
        {
            set = new HashSet<Collider>();
            insideColliders[victim] = set;
        }

        bool wasEmpty = set.Count == 0;
        set.Add(other);   // HashSet ignores duplicates automatically

        if (wasEmpty && set.Count == 1)
        {
            victim.AddSpeedModifier(this, speedMult);
            if (victim is PlayerMovement pm)
                pm.SetInMud(true);
        }
    }

    public void HandleExit(Collider other)
    {
        IMovement victim = other.GetComponentInParent<IMovement>();
        if (victim == null) return;

        if (!insideColliders.TryGetValue(victim, out var set)) return;

        set.Remove(other);

        // Prune any colliders destroyed mid-overlap.
        set.RemoveWhere(c => c == null);

        if (set.Count == 0)
        {
            victim.RemoveSpeedModifier(this);
            insideColliders.Remove(victim);
            if (victim is PlayerMovement pm)
                pm.SetInMud(false);
        }
    }
}