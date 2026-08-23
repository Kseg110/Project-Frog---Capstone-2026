using UnityEngine;

public class RapidfireUpgrade : MonoBehaviour, IElementUpgrade
{
    public static RapidfireUpgrade Instance { get; private set; }
    public AnchorElement Element => AnchorElement.Wind;

    private bool active = false;
    private float baseAPS;
    private PlayerAttacks pa;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        pa = FindFirstObjectByType<PlayerAttacks>();

        if (pa != null)
            baseAPS = pa.attacksPerSecond;
    }

    public void OnElementAttached(AnchorBase anchor)
    {
        pa = FindFirstObjectByType<PlayerAttacks>();
        if (pa == null) return;

        // Check if the upgrade is active
        active = UpgradeManager.Instance.HasUpgrade("Rapidfire");
        if (!active) return;

        float bonus = GetBonus();

        pa.attacksPerSecond = baseAPS * (1f + bonus / 100f);
    }

    public void OnElementDetached()
    {
        pa = FindFirstObjectByType<PlayerAttacks>();
        if (pa == null) return;

        active = false;

        pa.attacksPerSecond = baseAPS;
    }

    public float GetBonus()
    {
        return UpgradeManager.Instance.GetTotalStatForElement(
            AnchorElement.Wind,
            UpgradeStat.AttackSpeed
        );
    }
}