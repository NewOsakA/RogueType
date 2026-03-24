using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject prefab;
    public int unlockWave = 1;
    public int spawnWeight = 1;

    [Header("Special Enemy")]
    public bool isSpecial = false;
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

    [Header("Enemy Count Progression (Baseline)")]
    public int baseEnemyCount = 4;
    public int enemyCountIncrease = 2;

    [Header("HP Progression (Game Progression)")]
    public int hpIncreaseEveryXWaves = 11;
    public float hpIncreasePercent = 0.10f;

    [Header("Boss Scaling")]
    public bool bossUseWaveProgression = true;
    public bool bossUseDifficultyScaling = false;
    public float bossExtraHpMultiplier = 0.15f;
    
    private float timer;
    private float nextSpawnInterval;
    private int enemiesToSpawn;
    private bool waveActive = false;
    private bool isBossWave = false;
    private int currentWaveLocal = 1;

    public void BeginWave(int wave)
    {
        GameManager.Instance.RegisterSpawner();

        currentWaveLocal = wave;
        int waveIndex = Mathf.Max(0, wave - 1);

        waveActive = true;
        isBossWave = (wave % bossEveryXWaves == 0);

        var diff = GameManager.Instance.GetDifficulty();
        var modeProfile = GameManager.Instance.GetSelectedModeProfile();

        int baseline = baseEnemyCount + waveIndex * enemyCountIncrease;
        int modeSpawnBonus = modeProfile.enemySpawnFlatBonus + (waveIndex * modeProfile.enemySpawnBonusPerWave);

        enemiesToSpawn = isBossWave
            ? 1
            : Mathf.Max(0, baseline + diff.additionalEnemyCount + modeSpawnBonus);

        timer = 0f;

        nextSpawnInterval = GetNextInterval(diff);
    }

    void Update()
    {
        if (!waveActive) return;
        if (GameManager.Instance.IsBasePhase()) return;

        timer += Time.deltaTime;

        if (enemiesToSpawn > 0 && timer >= nextSpawnInterval)
        {
            SpawnEnemy(currentWaveLocal);
            enemiesToSpawn--;

            timer = 0f;

            var diff = GameManager.Instance.GetDifficulty();
            nextSpawnInterval = GetNextInterval(diff);
        }

        if (enemiesToSpawn <= 0 && waveActive)
        {
            waveActive = false;
            GameManager.Instance.NotifySpawnerFinished();
        }
    }

    float GetNextInterval(DifficultySettings diff)
    {
        float m = Mathf.Max(0.05f, diff.spawnRateMultiplier);
        return Random.Range(minSpawnInterval * m, maxSpawnInterval * m);
    }

    void SpawnEnemy(int wave)
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
                .Where(b => wave >= b.unlockWave && b.prefab != null)
                .ToList();

            if (availableBosses.Count == 0) return;

            int totalWeight = availableBosses.Sum(b => Mathf.Max(0, b.spawnWeight));
            if (totalWeight <= 0) return;

            int roll = Random.Range(0, totalWeight);
            int acc = 0;

            foreach (var boss in availableBosses)
            {
                acc += Mathf.Max(0, boss.spawnWeight);
                if (roll < acc)
                {
                    newEnemy = Instantiate(boss.prefab, spawnPos, Quaternion.identity);
                    GameManager.Instance.RegisterEnemy();
                    ApplyHpScaling(newEnemy, wave);
                    break;
                }
            }
        }
        else
        {
            var availableEnemies = enemyTypes
                .Where(e => wave >= e.unlockWave && e.prefab != null)
                .ToList();

            if (availableEnemies.Count == 0) return;

            var diff = GameManager.Instance.GetDifficulty();

            int GetWeight(EnemySpawnEntry e)
            {
                int w = Mathf.Max(0, e.spawnWeight);
                if (e.isSpecial)
                    w = Mathf.RoundToInt(w * diff.specialWeightMultiplier);
                return Mathf.Max(0, w);
            }

            int totalWeight = availableEnemies.Sum(e => GetWeight(e));
            if (totalWeight <= 0) return;

            int roll = Random.Range(0, totalWeight);
            int acc = 0;

            foreach (var entry in availableEnemies)
            {
                acc += GetWeight(entry);
                if (roll < acc)
                {
                    newEnemy = Instantiate(entry.prefab, spawnPos, Quaternion.identity);
                    GameManager.Instance.RegisterEnemy();
                    ApplyHpScaling(newEnemy, wave);
                    break;
                }
            }
        }
    }
    void ApplyHpScaling(GameObject enemyObj, int wave)
    {
        if (enemyObj == null) return;

        var enemy = enemyObj.GetComponent<Enemy>();
        if (enemy == null) return;

        var diff = GameManager.Instance.GetDifficulty();
        var modeProfile = GameManager.Instance.GetSelectedModeProfile();

        float finalMultiplier = 1f;

        if (enemy.isBoss)
        {
            int bossCount = wave / bossEveryXWaves;

            float bossScaling = Mathf.Pow(bossExtraHpMultiplier, bossCount);

            finalMultiplier = bossScaling;

            if (bossUseWaveProgression && hpIncreaseEveryXWaves > 0)
            {
                int step = wave / hpIncreaseEveryXWaves;
                finalMultiplier *= (1f + step * hpIncreasePercent);
            }

            if (bossUseDifficultyScaling)
            {
                finalMultiplier *= diff.hpMultiplier;
            }

            finalMultiplier *= modeProfile.enemyHealthMultiplier;

            Debug.Log($"Boss scaling x{finalMultiplier:F2}");
        }
        else
        {
            int step = (hpIncreaseEveryXWaves > 0)
                ? (wave / hpIncreaseEveryXWaves)
                : 0;

            float progressionMul = 1f + step * hpIncreasePercent;

            finalMultiplier = progressionMul * diff.hpMultiplier * modeProfile.enemyHealthMultiplier;
        }

        int scaledHp = Mathf.RoundToInt(enemy.maxHP * finalMultiplier);
        scaledHp = Mathf.Max(1, scaledHp);
        int scaledDamage = Mathf.Max(1, Mathf.RoundToInt(enemy.damage * modeProfile.enemyDamageMultiplier));
        float scaledSpeed = Mathf.Max(0.05f, enemy.speed * modeProfile.enemySpeedMultiplier);

        enemy.Initialize(
            scaledHp,
            scaledDamage,
            scaledSpeed,
            enemy.isBoss
        );
    }
}
