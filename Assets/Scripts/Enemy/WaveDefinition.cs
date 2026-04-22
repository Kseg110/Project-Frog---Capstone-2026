using UnityEngine;

/// <summary>
/// Defines the data for a single wave:
/// - Which enemy groups will spawn
/// - Which spawn zones will be used
/// This ScriptableObject is read by WaveRoundSystem.
/// </summary>
[System.Serializable]
public class EnemyGroup
{
    /// <summary>
    /// The enemy prefab to spawn for this group.
    /// </summary>
    public GameObject enemyPrefab;

    /// <summary>
    /// How many enemies of this type will spawn in the wave.
    /// </summary>
    public int count;
}

[CreateAssetMenu(menuName = "Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    /// <summary>
    /// All enemy groups that will spawn in this wave.
    /// Each group defines a prefab and a count.
    /// </summary>
    [Header("Enemies in this wave")]
    public EnemyGroup[] enemyGroups;

    /// <summary>
    /// Spawn points used by this wave.
    /// These must be scene Transforms assigned manually.
    /// </summary>
    [Header("Spawn zones used for this wave")]
    public Transform[] spawnZones;
}