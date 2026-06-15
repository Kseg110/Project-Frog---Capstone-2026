using UnityEngine;

public class ShatterUpgrade : MonoBehaviour, IElementUpgrade
{
    public static ShatterUpgrade Instance { get; private set; }
    public AnchorElement Element => AnchorElement.Ice;

    private void Awake()
    {
        Instance = this;
    }

    public void OnElementAttached(AnchorBase anchor) { }
    public void OnElementDetached() { }

    public void OnHitEnemy(EnemyBase enemy)
    {
        if (!UpgradeManager.Instance.HasUpgrade("Shatter"))
            return;

        if (enemy != null && enemy.IsFrozen)
        {
            enemy.TakeDamage(50);
            enemy.Cleanse();
        }
    }
}