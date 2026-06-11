using System;
using UnityEngine;

public class UpgradeEffectManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnchor playerAnchor;

    [SerializeField] private FireUpgradeSystem fireSystem;
    [SerializeField] private IceUpgradeSystem iceSystem;
    [SerializeField] private WindUpgradeSystem windSystem;

    private void OnEnable()
    {
        if (playerAnchor == null)
            playerAnchor = FindFirstObjectByType<PlayerAnchor>();

        if (playerAnchor == null)
        {
            Debug.LogError("[UpgradeEffectManager] PlayerAnchor reference is missing!");
            return;
        }

        playerAnchor.OnTetherStarted += HandleAttach;
        playerAnchor.OnTetherReleased += HandleDetach;

        Debug.Log("[UpgradeEffectManager] Subscribed to PlayerAnchor events.");
    }

    private void OnDisable()
    {
        if (playerAnchor != null)
        {
            playerAnchor.OnTetherStarted -= HandleAttach;
            playerAnchor.OnTetherReleased -= HandleDetach;

            Debug.Log("<color=yellow>[UpgradeEffectManager]</color> Unsubscribed from PlayerAnchor events.");
        }
    }

    private void HandleAttach(AnchorBase anchor)
    {
        Debug.Log($"<color=cyan>[UpgradeEffectManager]</color> Anchor attached: <b>{anchor.Element}</b>");

        fireSystem.enabled = anchor.Element == AnchorElement.Fire;
        iceSystem.enabled = anchor.Element == AnchorElement.Ice;
        windSystem.enabled = anchor.Element == AnchorElement.Wind;

        Debug.Log(
            $"<color=green>[UpgradeEffectManager]</color> Systems state → " +
            $"Fire: {fireSystem.enabled}, Ice: {iceSystem.enabled}, Wind: {windSystem.enabled}"
        );
    }

    private void HandleDetach()
    {
        Debug.Log("<color=magenta>[UpgradeEffectManager]</color> Anchor detached — disabling all systems.");

        fireSystem.enabled = false;
        iceSystem.enabled = false;
        windSystem.enabled = false;
    }
}