using System.Collections;
using UnityEngine;
//AJ

public class WaveManager : MonoBehaviour
{
    public int currentWave = 1;
    public float normalWaveDelay = 5f;
    public float bossWaveDelay = 60f; 

    public void StartNextWave()
    {
        StartCoroutine(HandleWaveDelay());
    }

    IEnumerator HandleWaveDelay()
    {
        if (currentWave % 5 == 0)
        {
            Debug.Log("Rest period before Wave " + currentWave);
            yield return new WaitForSeconds(bossWaveDelay);
        }
        else
        {
            yield return new WaitForSeconds(normalWaveDelay);
        }

        SpawnWave();
    }

    void SpawnWave()
    {
        Debug.Log("Starting Wave " + currentWave);


        currentWave++;
    }
}