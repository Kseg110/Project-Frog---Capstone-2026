using System.Collections.Generic;
using UnityEngine;


//Attach this to Empty parent gameObject & attach helper to child with colliders
public class MudPit : MonoBehaviour
{
    [Header("Slow strength")]
    [SerializeField] private float speedMult = -0.5f;

    // Use to track how many colliders of THIS mudpit each player/enemy is inside
    //private Dictionary<IMovement, int> insideCounts = new Dictionary<IMovement, int>();
    private Dictionary<MonoBehaviour, int> insideCounts = new Dictionary<MonoBehaviour, int>();

    public void HandleEnter(Collider other)
    {
        Debug.Log($"[MudPit] Physics detected trigger enter from: {other.gameObject.name}", other.gameObject);

        IMovement victim = other.GetComponentInParent<IMovement>();

        if (victim == null) return;

        MonoBehaviour victimComponent = victim as MonoBehaviour;

        if (victimComponent == null)
        {
            Debug.LogWarning($"[MudPit] Found collider {other.gameObject.name}, but COULD NOT find IMovement in its parents!", other.gameObject);
            return;
        }

        if (!insideCounts.ContainsKey(victimComponent))
        {
            insideCounts[victimComponent] = 0;
        }

        insideCounts[victimComponent]++;

        // Only apply debuff on first collider entered
        if (insideCounts[victimComponent] == 1)
        {
            Debug.Log($"[MudPit] Successfully applying speed modifier to {victimComponent.gameObject.name}!", victimComponent.gameObject);
            victim.AddSpeedModifier(this, speedMult);
        }
    }

    public void HandleExit(Collider other)
    {
        IMovement victim = other.GetComponentInParent<IMovement>();
        if (victim == null) return;

        MonoBehaviour victimComponent = victim as MonoBehaviour;
        if (victimComponent == null || !insideCounts.ContainsKey(victimComponent)) return;

        insideCounts[victimComponent]--;

        // Only remove debuff if not still inside another collider form same mudpit
        if (insideCounts[victimComponent] <= 0)
        {
            victim.RemoveSpeedModifier(this);
            insideCounts.Remove(victimComponent);
        }
    }
}
