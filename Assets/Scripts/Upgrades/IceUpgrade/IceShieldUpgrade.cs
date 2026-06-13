using UnityEngine;
using System.Collections;

public class IceShieldUpgrade : MonoBehaviour, IElementUpgrade
{
    public AnchorElement Element => AnchorElement.Ice;

    private PlayerShieldController shield;
    private PlayerAnchor anchor;

    private bool ready = true;
    private float cooldown = 1f;
    private float freezeRadius = 10f;

    private void Awake()
    {
        shield = FindFirstObjectByType<PlayerShieldController>();
        anchor = FindFirstObjectByType<PlayerAnchor>();
    }

    private void OnEnable()
    {
        shield.OnShieldBroken += HandleBreak;
    }

    private void OnDisable()
    {
        shield.OnShieldBroken -= HandleBreak;
    }

    public void OnElementAttached(AnchorBase a)
    {
        if (!UpgradeManager.Instance.HasUpgrade("Ice shield"))
            return;

        if (!ready) return;

        shield.GiveShield();
        ready = false;
        StartCoroutine(Cooldown());
    }

    public void OnElementDetached()
    {
        shield.RemoveShield();
    }

    private IEnumerator Cooldown()
    {
        yield return new WaitForSeconds(cooldown);
        ready = true;
    }

    private void HandleBreak()
    {
        if (!UpgradeManager.Instance.HasUpgrade("Ice shield"))
            return;

        FreezeExplosion();
    }

    private void FreezeExplosion()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);

        foreach (var e in enemies)
        {
            float dist = Vector3.Distance(anchor.transform.position, e.transform.position);
            if (dist <= freezeRadius)
                e.Freeze(1f);
        }
    }
}