// EnemySpawner.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject prefab;
    public int unlockWave = 1;
    public int spawnWeight = 1;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Types")]
    public List<EnemySpawnEntry> enemyTypes;
    public GameObject bossPrefab;
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
    private int aliveEnemies;
    private bool waveActive = false;
    private bool isBossWave = false;
    private bool allSpawned = false;

    public void BeginWave(int wave)
    {
        int waveIndex = Mathf.Max(0, wave - 1);
        waveActive = true;
        allSpawned = false;

        isBossWave = (wave % bossEveryXWaves == 0);
        enemiesToSpawn = isBossWave ? 1 : baseEnemyCount + waveIndex * enemyCountIncrease;
        aliveEnemies = 0;
        timer = 0f;
        nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    void Update()
    {
        if (!waveActive || GameManager.Instance.IsBasePhase())
            return;

        timer += Time.deltaTime;
        if (timer >= nextSpawnInterval && enemiesToSpawn > 0)
        {
            SpawnEnemy(GameManager.Instance.currentWave);
            enemiesToSpawn--;
            timer = 0f;
            nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);

            if (enemiesToSpawn <= 0)
            {
                allSpawned = true;
            }
        }
    }

    void SpawnEnemy(int currentWave)
    {
        Vector3 spawnPos = new Vector3(spawnX, Random.Range(minY, maxY), 0f);
        GameObject newEnemy = null;

        if (isBossWave && bossPrefab != null)
        {
            newEnemy = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            var availableEntries = enemyTypes
                .Where(e => currentWave >= e.unlockWave)
                .ToList();

            if (availableEntries.Count == 0) return;

            int totalWeight = availableEntries.Sum(e => e.spawnWeight);
            int random = Random.Range(0, totalWeight);
            int runningWeight = 0;

            foreach (var entry in availableEntries)
            {
                runningWeight += entry.spawnWeight;
                if (random < runningWeight)
                {
                    newEnemy = Instantiate(entry.prefab, spawnPos, Quaternion.identity);
                    break;
                }
            }
        }

        if (newEnemy != null)
        {
            aliveEnemies++;

            Enemy enemy = newEnemy.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.OnDeath += HandleEnemyDeath;
            }
        }
    }

    void HandleEnemyDeath()
    {
        aliveEnemies--;
        if (allSpawned && aliveEnemies <= 0)
        {
            waveActive = false;
            GameManager.Instance.EndWave();
        }
    }

    // Register enemies summoned by the boss
    public void RegisterExternalEnemy(Enemy enemy)
    {
        aliveEnemies++;
        enemy.OnDeath += HandleEnemyDeath;
    }
}
