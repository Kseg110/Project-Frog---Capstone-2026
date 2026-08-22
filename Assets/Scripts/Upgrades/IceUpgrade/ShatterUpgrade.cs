using UnityEngine;

public class ShatterUpgrade : MonoBehaviour, IElementUpgrade
{
    public static ShatterUpgrade Instance { get; private set; }
    public AnchorElement Element => AnchorElement.Ice;

    [SerializeField] private float bonusDamage = 50f;

    private void Awake()
    {
        Instance = this;
    }

    public void OnElementAttached(AnchorBase anchor) { }
    public void OnElementDetached() { }

    public float GetBonusDamage()
    {
        return bonusDamage;
    }

    public bool IsEnabled()
    {
        return UpgradeManager.Instance.HasUpgrade("Shatter");
    }

    public void TryApplyShatter(EnemyBase enemy, bool wasFrozenBeforeHit)
    {
        if (!IsEnabled())
            return;

        if (enemy != null && wasFrozenBeforeHit)
        {
            enemy.TakeDamage(bonusDamage);
        }
    }
}