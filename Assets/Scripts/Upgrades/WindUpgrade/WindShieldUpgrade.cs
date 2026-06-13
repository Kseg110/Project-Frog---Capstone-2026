using UnityEngine;

public class WindShieldUpgrade : MonoBehaviour, IElementUpgrade
{
    public AnchorElement Element => AnchorElement.Wind;

    private PlayerShieldController shield;
    private PlayerAnchor anchor;

    private int hp;
    private int maxHP = 2;

    private void Awake()
    {
        shield = FindFirstObjectByType<PlayerShieldController>();
        anchor = FindFirstObjectByType<PlayerAnchor>();
    }

    private void OnEnable()
    {
        shield.OnShieldBroken += HandleBreak;
    }

    private void OnDisable()
    {
        shield.OnShieldBroken -= HandleBreak;
    }

    public void OnElementAttached(AnchorBase a)
    {
        if (!UpgradeManager.Instance.HasUpgrade("Wind shield"))
            return;

        hp = maxHP;
        shield.GiveShield();
    }

    public void OnElementDetached()
    {
        hp = 0;
        shield.RemoveShield();
    }

    private void HandleBreak()
    {
        if (hp > 0)
        {
            hp--;
            if (hp > 0)
                shield.GiveShield();
        }
    }
}