using UnityEngine;

/// <summary>
/// Attach to the same enemy GameObject as EnemyBase.
/// Watches EnemyBase.IsAttacking and triggers the AttackIndicator
/// automatically — no changes to EnemyBase required beyond the
/// public bool IsAttacking => isAttacking; read-only property.
///
/// Setup:
///   1. Attach this script to your enemy GameObject.
///   2. Drag the AttackIndicator child GameObject into the Attack Indicator slot.
///   3. Set Windup Duration to match the attackDuration in EnemyBase.
/// </summary>
public class AttackIndicatorController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the AttackIndicator child GameObject here.")]
    public AttackIndicator attackIndicator;

    [Header("Settings")]
    [Tooltip("Should match the attackDuration in EnemyBase so the indicator\n" +
             "colour transition finishes exactly when the attack lands.")]
    public float windupDuration = 2f;

    //Internal

    private EnemyBase _enemyBase;

    // Tracks the previous frame's attack state so we can detect
    // the rising edge (attack started) and falling edge (attack ended)
    private bool _wasAttacking = false;

    private void Awake()
    {
        _enemyBase = GetComponent<EnemyBase>();

        if (_enemyBase == null)
            Debug.LogWarning("[AttackIndicatorController] No EnemyBase found on this GameObject.");

        if (attackIndicator == null)
            Debug.LogWarning("[AttackIndicatorController] No AttackIndicator assigned.");
    }

    private void Update()
    {
        if (_enemyBase == null || attackIndicator == null) return;

        bool isAttackingNow = _enemyBase.IsAttacking;

        // Rising edge — attack just started, trigger the windup animation
        if (isAttackingNow && !_wasAttacking)
        {
            attackIndicator.TriggerWindup(windupDuration);
            Debug.Log("[AttackIndicatorController] Attack started — triggering windup.");
        }

        // Falling edge — attack just ended or was interrupted, hide the indicator
        if (!isAttackingNow && _wasAttacking)
        {
            attackIndicator.Hide();
            Debug.Log("[AttackIndicatorController] Attack ended — hiding indicator.");
        }

        _wasAttacking = isAttackingNow;
    }
}