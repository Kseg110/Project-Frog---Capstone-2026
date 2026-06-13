using UnityEngine;

public class WindShieldUpgrade : MonoBehaviour, IElementUpgrade
{
    public AnchorElement Element => AnchorElement.Wind;

    private PlayerShieldController shield;

    private int charges = 2;

    private void Awake()
    {
        shield = FindFirstObjectByType<PlayerShieldController>();
    }

    public void OnElementAttached(AnchorBase anchor)
    {
        if (anchor.Element != AnchorElement.Wind) return;
        if (!UpgradeManager.Instance.HasUpgrade("Wind Shield")) return;

        shield.GiveWindShield(charges);
        Debug.Log("[Shield] Wind shield activated!");
    }

    public void OnElementDetached()
    {
        shield.RemoveShield();
    }
}