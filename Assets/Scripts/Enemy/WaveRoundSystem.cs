using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Manages the entire wave system:
/// - Starts waves
/// - Spawns enemies
/// - Detects when a wave is finished
/// - Shows the card selection UI
/// - Starts the next wave after a card is chosen
/// </summary>

public class WaveRoundSystem : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private WaveDefinition[] waves;
    
    [Header("Spawn Settings")]
    [SerializeField] private float delayBetweenSpawns = 0.2f;
    
    private readonly List<GameObject> activeEnemies = new List<GameObject>();

    private int currentWaveIndex = -1;
    public int CurrentWave => currentWaveIndex;
    private bool waitingForCardSelection = false;
    private bool waveInProgress = false;

    [SerializeField] private CardSelectionUI cardSelectionUI;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI waveText;

    /// <summary>
    /// Called by WaveStartTrigger when the player enters the trigger.
    /// Starts the very first wave of the game.
    /// </summary>
    
    public void StartFirstWave()
    {
        if (waveInProgress || currentWaveIndex >= 0) return;

        currentWaveIndex = 0;
        StartWave(currentWaveIndex);
    }

    // check if wave is over and show card selection
    private void Update()
    {
        //kill all enemies in the wave to test card selection
        if (Input.GetKeyDown(KeyCode.T))
        {
            KillAllEnemiesInWave();
        }

        if (waveInProgress &&
            !waitingForCardSelection && 
            activeEnemies.Count == 0)
        {
            waitingForCardSelection = true;
            waveInProgress = false;

            if (cardSelectionUI != null)
                cardSelectionUI.ShowCardSelectionFromWave();  
        }
    }

    /// <summary>
    /// Starts a wave by reading its WaveDefinition
    /// and spawning all enemy groups.
    /// </summary>
    
    private void StartWave(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= waves.Length)
        {
            Debug.LogError("Invalid wave index!");
            return;
            // load victory screen or end game logic here
        }
        WaveDefinition wave = waves[waveIndex];
        activeEnemies.Clear();
        waitingForCardSelection = false;
        waveInProgress = true;

        Debug.Log($"Starting Wave {waveIndex + 1} with {wave.enemyGroups.Length} enemy groups.");

        StartCoroutine(SpawnWaveEnemies(wave));

        UpdateWaveText();
    }

    /// <summary>
    /// Spawns all enemy groups in the wave with a delay between each enemy.
    /// </summary>

    private IEnumerator SpawnWaveEnemies(WaveDefinition wave)
    {
        foreach (EnemyGroup group in wave.enemyGroups)
        {
            for (int i = 0; i < group.count; i++)
            {
                SpawnEnemy(group.enemyPrefab, wave.spawnZones);
                yield return new WaitForSeconds(delayBetweenSpawns);
            }
        }
    }

    /// <summary>
    /// Spawns a single enemy at a random spawn zone
    /// and registers its death callback.
    /// </summary>

    private void SpawnEnemy(GameObject enemyPrefab, Transform[] spawnZones)
    {
        if (enemyPrefab == null || spawnZones == null || spawnZones.Length == 0)
        {
            Debug.LogWarning("Missing prefab or spawn zones on wave.");
            return;
        }

        Transform zone = spawnZones[Random.Range(0, spawnZones.Length)];
        GameObject enemy = Instantiate(enemyPrefab, zone.position, Quaternion.identity);
        activeEnemies.Add(enemy);

        Health hp = enemy.GetComponent<Health>();
        if (hp != null)
            hp.OnDestroyed += (deadEnemy) => HandleEnemyDeath(deadEnemy);
    }

    /// <summary>
    /// Removes an enemy from the active list when it dies.
    /// </summary>

    private void HandleEnemyDeath(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
            activeEnemies.Remove(enemy);
    }

    /// <summary>
    /// Called by CardSelectionUI after the player chooses a card.
    /// Starts the next wave in the sequence.
    /// </summary>

    public void StartNextWaveAfterCard()
    {
        currentWaveIndex++;

        if (currentWaveIndex >= waves.Length)
        {
            Debug.Log("All waves completed after card selection!");
            return;
        }

        StartWave(currentWaveIndex);
    }

    /// <summary>
    /// Debug tool: instantly kills all enemies in the current wave.
    /// </summary>

    private void KillAllEnemiesInWave()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Health hp = enemy.GetComponent<Health>();
                if (hp != null)
                {
                    hp.TakeDmg(999999f);
                }
                else
                {
                    Destroy(enemy);
                }
            }
        }

        activeEnemies.Clear();
    }

    /// <summary>
    /// Updates the UI text to show the current wave number.
    /// </summary>

    private void UpdateWaveText()
    {
        if (waveText != null)
        {
            waveText.text = $"Wave {currentWaveIndex + 1}";
        }
    }
}