using System.Collections;
using UnityEngine;

public class FireShieldUpgrade : MonoBehaviour, IElementUpgrade
{
    public AnchorElement Element => AnchorElement.Fire;

    private PlayerShieldController shield;
    private PlayerAnchor playerAnchor;

    private bool ready = true;
    [SerializeField] private float cooldown = 10f;

    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 6f;
    [SerializeField] private float explosionBaseDamage = 20f;

    private void Awake()
    {
        shield = FindFirstObjectByType<PlayerShieldController>();
        playerAnchor = FindFirstObjectByType<PlayerAnchor>();
    }

    private void OnEnable()
    {
        if (shield != null)
            shield.OnShieldBroken += HandleShieldBreak;
    }

    private void OnDisable()
    {
        if (shield != null)
            shield.OnShieldBroken -= HandleShieldBreak;
    }

    public void OnElementAttached(AnchorBase anchor)
    {
        if (anchor.Element != AnchorElement.Fire) return;
        if (!UpgradeManager.Instance.HasUpgrade("Fire shield")) return;
        if (!ready) return;

        shield?.GiveShield();
        ready = false;
        StartCoroutine(CooldownRoutine());
    }

    public void OnElementDetached()
    {
        shield?.RemoveShield();
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(cooldown);
        ready = true;
    }

    private void HandleShieldBreak()
    {
        if (!UpgradeManager.Instance.HasUpgrade("Fire shield"))
            return;

        TriggerExplosion();
    }

    private void TriggerExplosion()
    {
        float bonus = UpgradeManager.Instance.GetTotalStatForElement(
            AnchorElement.Fire,
            UpgradeStat.ExplosionDamage
        );

        float finalDamage = explosionBaseDamage * (1f + bonus / 100f);

        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        var fireAnchor = FindFirstObjectByType<AnchorFire>();
        float burnDuration = fireAnchor != null ? fireAnchor.Data.BurnDuration : 2f;
        float burnTickRate = fireAnchor != null ? fireAnchor.Data.BurnTickRate : 0.2f;

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(playerAnchor.transform.position, enemy.transform.position);
            if (dist <= explosionRadius)
            {
                enemy.TakeDamage(finalDamage, "Burn", burnDuration, burnTickRate);
            }
        }

        Debug.Log($"[FireShieldUpgrade] Explosion → {finalDamage} dmg");
    }

}