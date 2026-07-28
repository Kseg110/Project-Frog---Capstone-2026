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
        Debug.Log($"[MudPit] ENTER from: {other.gameObject.name}", other.gameObject);

        if ((affectedLayers.value & (1 << other.gameObject.layer)) == 0) return;

        IMovement victim = other.GetComponentInParent<IMovement>();
        if (victim == null) return;

        if (!insideColliders.TryGetValue(victim, out var set))
        {
            set = new HashSet<Collider>();
            insideColliders[victim] = set;
        }

        bool wasEmpty = set.Count == 0;
        set.Add(other);   // HashSet ignores duplicates automatically

        if (wasEmpty && set.Count == 1)
        {
            Debug.Log($"[MudPit] Applying speed modifier to {((MonoBehaviour)victim).gameObject.name}!", ((MonoBehaviour)victim).gameObject);
            victim.AddSpeedModifier(this, speedMult);
        }
    }

    public void HandleExit(Collider other)
    {
        Debug.Log($"[MudPit] EXIT from: {other.gameObject.name}", other.gameObject);

        IMovement victim = other.GetComponentInParent<IMovement>();
        if (victim == null) return;
        if (!insideColliders.TryGetValue(victim, out var set)) return;

        set.Remove(other);

        if (set.Count == 0)
        {
            victim.RemoveSpeedModifier(this);
            insideColliders.Remove(victim);
        }
        //insideColliders.Clear();
    }
}