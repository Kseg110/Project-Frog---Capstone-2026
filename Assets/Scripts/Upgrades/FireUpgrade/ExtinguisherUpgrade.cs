using UnityEngine;

public class ExtinguisherUpgrade : MonoBehaviour, IElementUpgrade
{
    public static ExtinguisherUpgrade Instance { get; private set; }

    public AnchorElement Element => AnchorElement.Fire;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void OnElementAttached(AnchorBase anchor) { }
    public void OnElementDetached() { }

    public void OnHitEnemy(EnemyBase enemy)
    {
        if (!UpgradeManager.Instance.HasUpgrade("Extinguisher"))
            return;

        if (enemy != null && enemy.IsBurning)
        {
            enemy.TakeDamage(30);
            enemy.Cleanse();
        }
    }

}