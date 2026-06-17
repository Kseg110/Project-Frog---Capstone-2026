using UnityEngine;

public class MultishotUpgrade : MonoBehaviour, IElementUpgrade
{
    public static MultishotUpgrade Instance { get; private set; }
    public AnchorElement Element => AnchorElement.Wind;

    private void Awake()
    {
        Instance = this;
    }

    public void OnElementAttached(AnchorBase anchor) { }
    public void OnElementDetached() { }

    public int GetExtraDarts()
    {
        return (int)UpgradeManager.Instance.GetTotalStatForElement(
            AnchorElement.Wind,
            UpgradeStat.ExtraVolleyDarts
        );
    }
}