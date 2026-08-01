using UnityEngine;

[CreateAssetMenu(fileName = "LobAttackSO", menuName = "Scriptable Objects/LobAttackSO")]
public class LobAttackSO : AttackBaseSO
{
    [Header("SO Metadata")]
    [SerializeField] private string _attackName = "Lob";
    [SerializeField] private float _range = 15f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _cooldown = 3f;

    [Header("Projectile")]
    [Tooltip("Projectile prefab must have a non-kinematic Rigidbody.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("Launch angle above horizontal (degrees).")]
    [SerializeField, Range(5f, 80f)] private float launchAngle = 45f;
    [Tooltip("Fallback speed if ballistic solution can't be computed.")]
    [SerializeField] private float fallbackSpeed = 12f;
    [Tooltip("Vertical offset (meters) added to the target position so projectiles aim higher than the feet.")]
    [SerializeField] private float landingYOffset = 1.2f;

    public override float range => _range;
    public override string attackName => _attackName;
    public override float damage => _damage;
    public override float cooldown => _cooldown;

    protected override void PerformAttack(Transform target, Transform enemy)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{name}: projectilePrefab not assigned.");
            return;
        }

        // Record landing position when attack starts, offset vertically so projectile aims a bit higher than feet.
        Vector3 landingPosBase = target != null ? target.position : enemy.position;
        Vector3 landingPos = landingPosBase + Vector3.up * landingYOffset;

        // Spawn point is on the enemy prefab. Try to find a child named "ProjectileSpawn" (case sensitive).
        Transform spawn = FindSpawnPointOnEnemy(enemy);
        Vector3 origin = spawn != null ? spawn.position : enemy.position;

        GameObject go = Object.Instantiate(projectilePrefab, origin, Quaternion.identity);

        // If prefab contains Projectile component, set damage and other flags (optional)
        var projComp = go.GetComponent<Projectile>();
        if (projComp != null)
        {
            projComp.isPlayerProjectile = false;
            projComp.damage = _damage;
        }

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"{name}: projectile prefab has no Rigidbody — cannot lob. Consider adding Rigidbody to prefab.");
            return;
        }

        Vector3 v = CalculateLaunchVelocity(origin, landingPos, launchAngle, Physics.gravity.y, fallbackSpeed);
        rb.linearVelocity = v;
        rb.useGravity = true;
    }

    // Look for a child transform named "ProjectileSpawn" on the enemy; return null if not found.
    private Transform FindSpawnPointOnEnemy(Transform enemy)
    {
        if (enemy == null) return null;

        // First try direct child lookup by name
        Transform child = enemy.Find("ProjectileSpawn");
        if (child != null) return child;

        // If not found, do a recursive search (case-insensitive)
        return RecursiveFindByName(enemy, "ProjectileSpawn");
    }

    private Transform RecursiveFindByName(Transform parent, string name)
    {
        foreach (Transform t in parent)
        {
            if (string.Equals(t.name, name, System.StringComparison.OrdinalIgnoreCase))
                return t;

            Transform found = RecursiveFindByName(t, name);
            if (found != null) return found;
        }
        return null;
    }

    // Calculates an initial velocity vector that will cause a projectile launched from origin
    // to pass through target at the requested elevation angle. If the exact solution is invalid,
    // returns a fallback straight shot with supplied fallbackSpeed.
    private Vector3 CalculateLaunchVelocity(Vector3 origin, Vector3 target, float angleDegrees, float gravityY, float fallbackSpeed)
    {
        Vector3 toTarget = target - origin;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float dist = toTargetXZ.magnitude;
        float y = toTarget.y;

        float angle = Mathf.Deg2Rad * Mathf.Clamp(angleDegrees, 1f, 89f);
        float g = Mathf.Abs(gravityY);

        float cos = Mathf.Cos(angle);
        float cos2 = cos * cos;
        float tan = Mathf.Tan(angle);

        // v^2 = g * d^2 / (2 * cos^2(angle) * (d * tan(angle) - y))
        float denom = 2f * cos2 * (dist * tan - y);

        if (denom <= 0f || dist < 0.001f)
        {
            // fallback: aim directly at target with a heuristic speed
            Vector3 dir = (toTargetXZ.normalized * Mathf.Cos(angle)) + Vector3.up * Mathf.Sin(angle);
            return dir.normalized * fallbackSpeed;
        }

        float vSq = g * dist * dist / denom;
        if (vSq <= 0f)
        {
            Vector3 dir = (toTargetXZ.normalized * Mathf.Cos(angle)) + Vector3.up * Mathf.Sin(angle);
            return dir.normalized * fallbackSpeed;
        }

        float v = Mathf.Sqrt(vSq);
        Vector3 velocity = toTargetXZ.normalized * v * Mathf.Cos(angle) + Vector3.up * v * Mathf.Sin(angle);
        return velocity;
    }
}