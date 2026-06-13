using UnityEngine;

public class IceShieldUpgrade : MonoBehaviour, IElementUpgrade
{
    public AnchorElement Element => AnchorElement.Ice;

    private PlayerShieldController shield;

    private void Awake()
    {
        shield = FindFirstObjectByType<PlayerShieldController>();
    }

    public void OnElementAttached(AnchorBase anchor)
    {
        if (anchor.Element != AnchorElement.Ice) return;
        if (!UpgradeManager.Instance.HasUpgrade("Ice shield")) return;

        shield.GiveIceShield();
    }

    public void OnElementDetached()
    {
        shield.RemoveShield();
    }
}