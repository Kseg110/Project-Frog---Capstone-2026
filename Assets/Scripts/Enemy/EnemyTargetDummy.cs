using UnityEngine;

public class DummyEnemy : EnemyBase
{
    protected override void Awake()
    {
        base.Awake();

        // prevent any movement by disabling Rigidbody physics
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;   // ignore AddForce / MovePosition
            rb.constraints = RigidbodyConstraints.FreezeAll; // never move
        }

        // Disable EnemyKnockback if present
        var knock = GetComponent<EnemyKnockback>();
        if (knock != null)
        {
            knock.enabled = false;
        }
    }

    // Override to prevent any forced movement
    public override void MoveTo(Vector3 destination)
    {
        // Do nothing
    }

    public override void StopMovement()
    {
        // Do nothing
    }

    public override void ResumeMovement()
    {
        // Do nothing
    }
}