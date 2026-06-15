using UnityEngine;

public class PointBlankShotUpgrade : MonoBehaviour, IElementUpgrade
{
    public static PointBlankShotUpgrade Instance { get; private set; }
    public AnchorElement Element => AnchorElement.Wind;

    private void Awake()
    {
        Instance = this;
    }

    public void OnElementAttached(AnchorBase anchor) { }
    public void OnElementDetached() { }

    public float GetBonus()
    {
        return UpgradeManager.Instance.GetTotalStatForElement(
            AnchorElement.Wind,
            UpgradeStat.PointBlankDamage
        );
    }
}