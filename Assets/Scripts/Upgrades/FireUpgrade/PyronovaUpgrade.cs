using UnityEngine;

public class PyronovaUpgrade : MonoBehaviour, IElementUpgrade
{
    public static PyronovaUpgrade Instance { get; private set; }

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

    public float GetExplosionBonus()
    {
        return UpgradeManager.Instance.GetTotalStatForElement(
            AnchorElement.Fire,
            UpgradeStat.ExplosionDamage
        );
    }
}