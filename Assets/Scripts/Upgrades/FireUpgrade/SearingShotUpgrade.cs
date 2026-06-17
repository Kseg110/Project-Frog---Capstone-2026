using UnityEngine;

public class SearingShotUpgrade : MonoBehaviour, IElementUpgrade
{
    public static SearingShotUpgrade Instance { get; private set; }

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

    public float GetDartBonus()
    {
        return UpgradeManager.Instance.GetTotalStatForElement(
            AnchorElement.Fire,
            UpgradeStat.DartDamage
        );
    }

}