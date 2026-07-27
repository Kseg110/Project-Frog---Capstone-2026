using UnityEngine;

[CreateAssetMenu(fileName = "BurrowAttackData", menuName = "Anchorbound/Attacks/Burrow Attack Data")]
public class EnemyBurrowAttackDataSO : ScriptableObject
{
    [Header("Burrow Settings")]
    public float burrowDepth = 2.5f;
    public float travelSpeed = 8f;
    public float maxTravelDuration = 5f;

    [Header("Emerge Settings")]
    public float emergeSpeed = 12f;
    public float postEmergePause = 0.5f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;

    [Header("Damage")]
    public float damage = 15f;
    public float damageRadius = 1.5f;
    public LayerMask playerLayer;

    [Header("Debug")]
    [Tooltip("Editor testing only — keeps the projectile mesh visible while underground so you can watch it travel.")]
    public bool debugVisibleWhileBurrowing = false;
}
