// TowerTypeManager
// Defines the serializable data classes for each tower type (Fire, Ice, Wind) and the
// TowerType enum. Each field class encapsulates its own validated properties and is used
// by GrappleTowerManager to configure per-tower behaviour exposed to AnchorDamageManager.

using UnityEngine;

[System.Serializable]
public class FireTowerFields
{
    [SerializeField] private float damage = 10f;

    [Header("Burn Settings")]
    [SerializeField] private float burnDuration = 2f;   // How long burn lasts
    [SerializeField] private float burnTickRate = 0.2f; // How often damage ticks

    public float Damage
    {
        get => damage;
        set => damage = Mathf.Max(0f, value);
    }
    public float BurnDuration
    {
        get => burnDuration;
        set => burnDuration = Mathf.Max(0f, value);
    }
    public float BurnTickRate
    {
        get => burnTickRate;
        set => burnTickRate = Mathf.Max(0.01f, value); // Prevent division by zero
    }
}

[System.Serializable]
public class IceTowerFields
{
    [SerializeField] private float damage = 8f;

    [Header("Ice Settings")]
    [SerializeField] private float slowMultiplier = 0.5f;  // 50% movement speed
    [SerializeField] private float slowDuration = 2f;
    [SerializeField] private float damageMultiplier = 2f;  // x2 damage

    public float Damage
    {
        get => damage;
        set => damage = Mathf.Max(0f, value);
    }
    public float SlowMultiplier
    {
        get => slowMultiplier;
        set => slowMultiplier = Mathf.Clamp(value, 0f, 1f); // 0 = full stop, 1 = no slow
    }
    public float SlowDuration
    {
        get => slowDuration;
        set => slowDuration = Mathf.Max(0f, value);
    }
    public float DamageMultiplier
    {
        get => damageMultiplier;
        set => damageMultiplier = Mathf.Max(0f, value);
    }
}

[System.Serializable]
public class IceTowerFields2
{
    [SerializeField] private float damage = 8f;

    [Header("Ice Settings")]
    [SerializeField] private float slowMultiplier = 0.5f;  // 50% movement speed
    [SerializeField] private float slowDuration = 2f;
    [SerializeField] private float damageMultiplier = 2f;  // x2 damage

    public float Damage
    {
        get => damage;
        set => damage = Mathf.Max(0f, value);
    }
    public float SlowMultiplier
    {
        get => slowMultiplier;
        set => slowMultiplier = Mathf.Clamp(value, 0f, 1f);
    }
    public float SlowDuration
    {
        get => slowDuration;
        set => slowDuration = Mathf.Max(0f, value);
    }
    public float DamageMultiplier
    {
        get => damageMultiplier;
        set => damageMultiplier = Mathf.Max(0f, value);
    }
}

[System.Serializable]
public class WindTowerFields
{
    [SerializeField] private float damage = 12f;

    [Header("Wind Settings")]
    [SerializeField] private float damageMultiplier = 0.7f; // 30% less damage (0.7x)

    public float Damage
    {
        get => damage;
        set => damage = Mathf.Max(0f, value);
    }
    public float DamageMultiplier
    {
        get => damageMultiplier;
        set => damageMultiplier = Mathf.Max(0f, value);
    }
}

public enum TowerType
{
    Fire,
    Ice,
    Wind
}

