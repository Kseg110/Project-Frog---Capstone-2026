using UnityEngine;

public static class CollisionUtility
{
    public static void MoveWithCapsuleCollision(
        Rigidbody rb,
        CapsuleCollider capsule,
        Vector3 motion,
        LayerMask collisionLayers,
        int maxIterations = 5,
        float skin = 0.01f)
    {
        if (motion.sqrMagnitude < 0.000001f)
            return;

        Vector3 remaining = motion;

        for (int i = 0; i < maxIterations; i++)
        {
            if (remaining.sqrMagnitude < 0.000001f)
                break;

            GetCapsule(rb, capsule, out Vector3 start, out Vector3 end);

            if (!Physics.CapsuleCast(
                    start,
                    end,
                    capsule.radius,
                    remaining.normalized,
                    out RaycastHit hit,
                    remaining.magnitude,
                    collisionLayers,
                    QueryTriggerInteraction.Ignore))
            {
                rb.MovePosition(rb.position + remaining);
                break;
            }

            float moveDist = Mathf.Max(hit.distance - skin, 0f);

            if (moveDist > 0f)
                rb.MovePosition(rb.position + remaining.normalized * moveDist);

            remaining = Vector3.ProjectOnPlane(remaining, hit.normal);

            if (hit.distance <= skin)
                break;
        }
    }

    public static void GetCapsule(Rigidbody rb, CapsuleCollider capsule, out Vector3 start, out Vector3 end)
    {
        Vector3 up = Vector3.up;

        start =
            rb.position +
            capsule.center +
            up * (capsule.height / 2f - capsule.radius);

        end =
            rb.position +
            capsule.center -
            up * (capsule.height / 2f - capsule.radius);
    }
}   