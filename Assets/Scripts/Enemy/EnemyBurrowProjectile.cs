using System.Collections;
using UnityEngine;

public class EnemyBurrowProjectile : MonoBehaviour
{
    [SerializeField] private Collider[] projectileColliders;

    private EnemyBurrowAttackDataSO data;
    private Vector3 targetGroundPos; // target's last-known position, fixed at spawn

    public void Initialize(Vector3 origin, Vector3 targetPosition, EnemyBurrowAttackDataSO attackData)
    {
        data = attackData;
        targetGroundPos = GetGroundPosition(targetPosition);
        transform.position = origin;
        StartCoroutine(BurrowRoutine());
    }

    private IEnumerator BurrowRoutine()
    {
        // 1. Submerge (stays visible if debug flag is on, for editor testing)
        SetVisible(data.debugVisibleWhileBurrowing);
        transform.position -= Vector3.up * data.burrowDepth;

        Vector3 undergroundTarget = targetGroundPos - (Vector3.up * data.burrowDepth);

        // 2. Travel underground to the fixed target (no re-tracking)
        float elapsed = 0f;
        while (elapsed < data.maxTravelDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.MoveTowards(
                transform.position, undergroundTarget, data.travelSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, undergroundTarget) < 0.2f)
                break;

            yield return null;
        }

        // 3. Emerge at the target's feet
        SetVisible(true);
        while (Vector3.Distance(transform.position, targetGroundPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, targetGroundPos, data.emergeSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetGroundPos;

        // 4. Deal damage at emerge point
        DealDamage();

        // 5. Cleanup
        yield return new WaitForSeconds(data.postEmergePause);
        Destroy(gameObject);
    }

    private void DealDamage()
    {
        Collider[] hits = Physics.OverlapSphere(
            targetGroundPos, data.damageRadius, data.playerLayer);

        foreach (var col in hits)
        {
            if (col.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDmg(data.damage);
        }
    }

    private void SetVisible(bool visible)
    {
        if (projectileColliders != null)
        {
            foreach (var c in projectileColliders)
                if (c != null) c.enabled = visible;
        }
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = visible;
    }

    private Vector3 GetGroundPosition(Vector3 rawPos)
    {
        Vector3 rayStart = rawPos + Vector3.up * 10f;
        if (data != null && data.groundLayer != 0 &&
            Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, data.groundLayer))
        {
            return hit.point;
        }
        return rawPos;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}