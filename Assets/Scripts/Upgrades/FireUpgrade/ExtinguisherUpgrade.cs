using UnityEngine;

public class ExtinguisherUpgrade : MonoBehaviour, IElementUpgrade
{
    public static ExtinguisherUpgrade Instance { get; private set; }

    public AnchorElement Element => AnchorElement.Fire;

    [Header("Extinguisher Settings")]
    [SerializeField] private float bonusDamage = 30f;

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

    public float GetBonusDamage()
    {
        return bonusDamage;
    }

    public bool IsEnabled()
    {
        return UpgradeManager.Instance.HasUpgrade("Extinguisher");
    }
}