using UnityEngine;

public class HomingDartsUpgrade : MonoBehaviour, IElementUpgrade
{
    public static HomingDartsUpgrade Instance { get; private set; }
    public AnchorElement Element => AnchorElement.Wind;

    private bool active = false;

    private void Awake()
    {
        Instance = this;
    }

    public void OnElementAttached(AnchorBase anchor)
    {
        active = UpgradeManager.Instance.HasUpgrade("Homing Darts");
    }

    public void OnElementDetached()
    {
        active = false;
    }

    public bool IsEnabled()
    {
        return active;
    }
}