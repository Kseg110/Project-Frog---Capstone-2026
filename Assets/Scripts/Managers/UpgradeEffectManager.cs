using UnityEngine;

public class UpgradeEffectManager : MonoBehaviour
{
    [SerializeField] private PlayerAnchor playerAnchor;

    private IElementUpgrade[] upgrades;

    private void Awake()
    {
        if (playerAnchor == null)
            playerAnchor = FindFirstObjectByType<PlayerAnchor>();

        upgrades = GetComponentsInChildren<IElementUpgrade>(true);
    }

    private void OnEnable()
    {
        if (playerAnchor == null) return;

        playerAnchor.OnTetherStarted += HandleAttach;
        playerAnchor.OnTetherReleased += HandleDetach;
    }

    private void OnDisable()
    {
        if (playerAnchor == null) return;

        playerAnchor.OnTetherStarted -= HandleAttach;
        playerAnchor.OnTetherReleased -= HandleDetach;
    }

    private void HandleAttach(AnchorBase anchor)
    {
        foreach (var up in upgrades)
        {
            if (up.Element == anchor.Element)
                up.OnElementAttached(anchor);
            else
                up.OnElementDetached();
        }
    }

    private void HandleDetach()
    {
        foreach (var up in upgrades)
            up.OnElementDetached();
    }
}