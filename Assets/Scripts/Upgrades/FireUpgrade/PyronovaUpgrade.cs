using UnityEngine;

public class PyronovaUpgrade : MonoBehaviour, IElementUpgrade
{
    public static PyronovaUpgrade Instance { get; private set; }

    public AnchorElement Element => AnchorElement.Fire;

    [Header("AOE Settings")]
    [SerializeField] private float aoeRadius = 5f;        
    [SerializeField] private float aoeDamagePercent = 50f;  

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

    public float GetExplosionBonus()
    {
        return UpgradeManager.Instance.GetTotalStatForElement(
            AnchorElement.Fire,
            UpgradeStat.ExplosionDamage
        );
    }

    /// <summary>
    /// Radius of the AOE explosion around the main target.
    /// </summary>
    public float GetAoeRadius()
    {
        return aoeRadius;
    }

    /// <summary>
    /// Percentage of the main explosion damage applied to AOE targets.
    /// </summary>
    public float GetAoeDamagePercent()
    {
        return aoeDamagePercent;
    }
}