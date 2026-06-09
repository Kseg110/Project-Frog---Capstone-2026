using UnityEngine;

public class UpgradeEffectManager : MonoBehaviour
{
    private AnchorTether tether;

    private FireUpgradeSystem fireSystem;
    private IceUpgradeSystem iceSystem;
    private WindUpgradeSystem windSystem;

    private void Awake()
    {
        tether = FindFirstObjectByType<AnchorTether>();
        fireSystem = FindFirstObjectByType<FireUpgradeSystem>();
        iceSystem = FindFirstObjectByType<IceUpgradeSystem>();
        windSystem = FindFirstObjectByType<WindUpgradeSystem>();
    }

    private void OnEnable()
    {
        tether.OnAnchorAttached += HandleAttach;
        tether.OnAnchorDetached += HandleDetach;
    }

    private void OnDisable()
    {
        tether.OnAnchorAttached -= HandleAttach;
        tether.OnAnchorDetached -= HandleDetach;
    }

    private void HandleAttach(AnchorBase anchor)
    {
        fireSystem.enabled = anchor.Element == AnchorElement.Fire;
        iceSystem.enabled = anchor.Element == AnchorElement.Ice;
        windSystem.enabled = anchor.Element == AnchorElement.Wind;
    }

    private void HandleDetach()
    {
        fireSystem.enabled = false;
        iceSystem.enabled = false;
        windSystem.enabled = false;
    }
}