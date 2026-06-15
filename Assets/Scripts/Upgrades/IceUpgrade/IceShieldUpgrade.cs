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
        if (!UpgradeManager.Instance.HasUpgrade("Ice Shield")) return;

        shield.GiveIceShield();
        Debug.Log("[Shield] ICE shield activated!");
    }

    public void OnElementDetached()
    {
        shield.RemoveShield();
    }
}