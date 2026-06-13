using UnityEngine;

public class FireShieldUpgrade : MonoBehaviour, IElementUpgrade
{
    public AnchorElement Element => AnchorElement.Fire;

    private PlayerShieldController shield;

    private void Awake()
    {
        shield = FindFirstObjectByType<PlayerShieldController>();
    }

    public void OnElementAttached(AnchorBase anchor)
    {
        if (anchor.Element != AnchorElement.Fire) return;
        if (!UpgradeManager.Instance.HasUpgrade("Fire Shield")) return;

        shield.GiveFireShield();
        Debug.Log("[Shield] FIRE shield activated!");
    }

    public void OnElementDetached()
    {
        shield.RemoveShield();
    }
}