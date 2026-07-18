using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Transactions;

public class EnemySpawnWaves : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject enemyPrefab;

    public Transform PlayerTransform => player;
    public int CurrentWaveNumber => currentWave;

    [Header("Wave Settings")]
    [SerializeField] private int enemiesPerWave = 8;
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float undergroundOffset = 2f;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private int currentWave;

    private bool isSpawningWave = false;
    private bool isWaitingForAreaTransition = false;

    private readonly List<GameObject> aliveEnemies = new List<GameObject>();

    private void Start()
    {
        currentWave = 1;
        SpawnNextWave();
    }

    public void StartSpawning()
    {
        StartCoroutine(WaitAndSpawnNextWave(true));
    }

    public void SpawnNextWave()
    {
        aliveEnemies.Clear();
        //Increase enemy count by wave number. Can be easily configured to set amounts
        if (currentWave <= 3)
        {
            enemiesPerWave += 2;
        }
        else if (currentWave <= 6)
        {
            enemiesPerWave += 3;
        }
        else
        {
            enemiesPerWave += 4;
        }

        for (int i = 0; i < enemiesPerWave; i++)
        {
            SpawnEnemy();
        }
    }
    

    private void SpawnEnemy()
    {
        //spawn randomly around the player, and underground.
        Vector3 spawnPosition = GetRandomPositionAroundPlayer();
        spawnPosition.y -= undergroundOffset;

        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        aliveEnemies.Add(enemy);
    }


    private Vector3 GetRandomPositionAroundPlayer()
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(3f, spawnRadius);
        Vector3 offset = new Vector3(randomCircle.x, 0f, randomCircle.y);

        return player.position + offset;
    }

    public void OnEnemyKilled(GameObject enemy)
    {
        /*
        aliveEnemies.Remove(enemy);

        if (aliveEnemies.Count == 0)
        {
            StartCoroutine(WaitAndSpawnNextWave());
        }   
        */

        if (aliveEnemies.Contains(enemy))
        {
            aliveEnemies.Remove(enemy);
        }

        aliveEnemies.RemoveAll(item => item == null);

        if (aliveEnemies.Count == 0 && !isSpawningWave)
        {
            // Check if the eave that just finished 
            if (currentWave == 5  || currentWave == 6 || currentWave == 10 || currentWave == 11 || currentWave == 15 || currentWave == 16)
            {
                PrepareAreaTransition();
            }
            else
            {
                StartCoroutine(WaitAndSpawnNextWave(false));
            }
        }
    }

    private void PrepareAreaTransition()
    {
        isWaitingForAreaTransition = true;
        Debug.Log($"Wave {currentWave} cleared!");

        // Door Logic ...
    }

    public void CompleteAreaTransition()
    {
        if (!isWaitingForAreaTransition) return;

        isWaitingForAreaTransition = false;
        Debug.Log("Player reached new area!");

        StartCoroutine(WaitAndSpawnNextWave(false));
    }

    public void OnDoorClosedBehindPlayer()
    {
        if (isWaitingForAreaTransition)
        {
            isWaitingForAreaTransition = false;
            currentWave++;
            StartCoroutine(WaitAndSpawnNextWave(true));   
        }
    }

    private IEnumerator WaitAndSpawnNextWave(bool isFirstWaveOfArea)
    {
        isSpawningWave = true;
        yield return new WaitForSeconds(timeBetweenWaves);

        if (!isFirstWaveOfArea)
        {
            currentWave++;
        }
        
        SpawnNextWave();
    }
}
