// EnemySpawner.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject prefab;
    public int unlockWave = 1;
    public int spawnWeight = 1;
}

[System.Serializable]
public class BossSpawnEntry
{
    public GameObject prefab;
    public int unlockWave = 5;
    public int spawnWeight = 1;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Types")]
    public List<EnemySpawnEntry> enemyTypes;

    [Header("Boss Types")]
    public List<BossSpawnEntry> bossTypes;
    public int bossEveryXWaves = 5;

    [Header("Spawn Settings")]
    public float minY = -3f;
    public float maxY = 3f;
    public float spawnX = 10f;
    public float minSpawnInterval = 0.5f;
    public float maxSpawnInterval = 2f;

    [Header("Enemy Scaling")]
    public int baseEnemyCount = 5;
    public int enemyCountIncrease = 2;

    private float timer;
    private float nextSpawnInterval;
    private int enemiesToSpawn;
    private bool waveActive = false;
    private bool isBossWave = false;

    public void BeginWave(int wave)
    {
        int waveIndex = Mathf.Max(0, wave - 1);

        waveActive = true;
        isBossWave = (wave % bossEveryXWaves == 0);

        enemiesToSpawn = isBossWave
            ? 1
            : baseEnemyCount + waveIndex * enemyCountIncrease;

        timer = 0f;
        nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    void Update()
    {
        if (!waveActive || GameManager.Instance.IsBasePhase())
            return;

        if (enemiesToSpawn <= 0)
        {
            waveActive = false; // stop spawning
            return;
        }

        timer += Time.deltaTime;
        if (timer >= nextSpawnInterval)
        {
            SpawnEnemy(GameManager.Instance.currentWave);
            enemiesToSpawn--;

            timer = 0f;
            nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }

    void SpawnEnemy(int currentWave)
    {
        Vector3 spawnPos = new Vector3(
            spawnX,
            Random.Range(minY, maxY),
            0f
        );

        GameObject newEnemy = null;

        if (isBossWave)
        {
            var availableBosses = bossTypes
                .Where(b => currentWave >= b.unlockWave)
                .ToList();

            if (availableBosses.Count == 0) return;

            int totalWeight = availableBosses.Sum(b => b.spawnWeight);
            int roll = Random.Range(0, totalWeight);

            int acc = 0;
            foreach (var boss in availableBosses)
            {
                acc += boss.spawnWeight;
                if (roll < acc)
                {
                    newEnemy = Instantiate(boss.prefab, spawnPos, Quaternion.identity);
                    break;
                }
            }
        }
        else
        {
            var availableEnemies = enemyTypes
                .Where(e => currentWave >= e.unlockWave)
                .ToList();

            if (availableEnemies.Count == 0) return;

            int totalWeight = availableEnemies.Sum(e => e.spawnWeight);
            int roll = Random.Range(0, totalWeight);

            int acc = 0;
            foreach (var entry in availableEnemies)
            {
                acc += entry.spawnWeight;
                if (roll < acc)
                {
                    newEnemy = Instantiate(entry.prefab, spawnPos, Quaternion.identity);
                    break;
                }
            }
        }
    }
}
