using UnityEngine;

public class WildfireUpgrade : MonoBehaviour, IElementUpgrade
{
    public static WildfireUpgrade Instance { get; private set; }
    public AnchorElement Element => AnchorElement.Fire;

    private bool active = false;

    private void Awake()
    {
        Instance = this;
    }

    public void OnElementAttached(AnchorBase anchor)
    {
        active = UpgradeManager.Instance.HasUpgrade("Wildfire");
    }

    public void OnElementDetached()
    {
        active = false;
    }

    public float GetBurnBonus()
    {
        if (!active) return 0f;

        return UpgradeManager.Instance.GetTotalStatForElement(
            AnchorElement.Fire,
            UpgradeStat.FireDamage
        );
    }
}