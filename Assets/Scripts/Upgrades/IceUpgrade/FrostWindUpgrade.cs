using UnityEngine;

public class FrostwindUpgrade : MonoBehaviour, IElementUpgrade
{
    public static FrostwindUpgrade Instance { get; private set; }
    public AnchorElement Element => AnchorElement.Ice;

    private void Awake()
    {
        Instance = this;
    }

    public void OnElementAttached(AnchorBase anchor) { }
    public void OnElementDetached() { }

    public float GetBonus()
    {
        return UpgradeManager.Instance.GetTotalStatForElement(
            AnchorElement.Ice,
            UpgradeStat.PrimarySpeed
        );
    }
}