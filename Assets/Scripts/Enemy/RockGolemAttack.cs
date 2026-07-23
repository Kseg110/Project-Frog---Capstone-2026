using System.Collections;
using UnityEngine;

public class RockGolemAttack : EnemyAttack
{
    [Header("Rock Golem Configuration")]
    [SerializeField] private GameObject previewPrefab;
    [SerializeField] private GameObject attackCylinderPrefab;
    [SerializeField] private float windupTime = 2f;
    [SerializeField] private float riseHeight = 3f;
    [SerializeField] private float riseSpeed = 6f;
    [SerializeField] private float lingeringDuration = 1f;
    [SerializeField] private LayerMask groundLayer;

    private GameObject activePreview;
    private GameObject activeCylinder;

    private void OnDisable()
    {
        if (activePreview != null) Destroy(activePreview);
        if (activeCylinder != null) Destroy(activeCylinder);
        IsAttacking = false;
    }

    protected override void OnExecuteAttack(Vector3 targetPosition)
    {
        StartCoroutine(GolemRoutine(targetPosition));
    }

    private IEnumerator GolemRoutine(Vector3 targetPosition)
    {
        IsAttacking = true;
        Vector3 strikePosition = GetGroundPosition(targetPosition);

        // 1. Spawn Preview Indicator
        if (previewPrefab != null)
        {
            activePreview = Instantiate(previewPrefab, strikePosition, Quaternion.identity);    
        }

        yield return new WaitForSeconds(windupTime);

        if (activePreview != null) Destroy(activePreview);

        // 2. Spawn and Raise Cylinder
        if (attackCylinderPrefab != null)
        {
            Vector3 startPos = strikePosition - (Vector3.up * riseHeight);
            Vector3 endPos = strikePosition;

            activeCylinder = Instantiate(attackCylinderPrefab, startPos, Quaternion.identity);

            float duration = riseHeight / Mathf.Max(riseSpeed, 0.01f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (activeCylinder == null) break;

                elapsed += Time.deltaTime;
                float percent = Mathf.Clamp01(elapsed / duration);
                activeCylinder.transform.position = Vector3.Lerp(startPos, endPos, percent);
                yield return null;
            }

            if (activeCylinder != null)
            {
                Destroy(activeCylinder, lingeringDuration);
            }
        }

        IsAttacking = false;
    }

    private Vector3 GetGroundPosition(Vector3 rawTargetPos)
    {
        if (groundLayer != 0 && Physics.Raycast(rawTargetPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, groundLayer))
        {
            return hit.point;
        }
        return rawTargetPos;
    }
}
