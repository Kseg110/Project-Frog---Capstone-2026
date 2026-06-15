using UnityEngine;

public class RapidfireUpgrade : MonoBehaviour, IElementUpgrade
{
    public static RapidfireUpgrade Instance { get; private set; }
    public AnchorElement Element => AnchorElement.Wind;

    private bool active = false;
    private float baseAPS;

    private void Awake()
    {
        Instance = this;

        // Get the base APS from the PlayerAttacks component
        PlayerAttacks pa = FindFirstObjectByType<PlayerAttacks>();
        if (pa != null)
            baseAPS = pa.attacksPerSecond;
    }

    public void OnElementAttached(AnchorBase anchor)
    {
        // Check if the upgrade is active
        active = UpgradeManager.Instance.HasUpgrade("Rapidfire");
        if (!active) return;

        float bonus = GetBonus();

        PlayerAttacks pa = FindFirstObjectByType<PlayerAttacks>();
        if (pa != null)
        {
            // Modify APS based on the bonus percentage
            pa.attacksPerSecond = baseAPS * (1f + bonus / 100f);
        }
    }

    public void OnElementDetached()
    {
        active = false;

        // Reset APS to the base value
        PlayerAttacks pa = FindFirstObjectByType<PlayerAttacks>();
        if (pa != null)
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