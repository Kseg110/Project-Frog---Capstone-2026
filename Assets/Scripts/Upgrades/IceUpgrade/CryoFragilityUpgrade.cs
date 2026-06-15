using UnityEngine;

public class CryoFragilityUpgrade : MonoBehaviour, IElementUpgrade
{
    public static CryoFragilityUpgrade Instance { get; private set; }
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
            UpgradeStat.DamageTakenIfSlowed
        );
    }
}