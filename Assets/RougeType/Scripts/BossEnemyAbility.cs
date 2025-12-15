using UnityEngine;

public class BossEnemyAbility : EnemyAbility
{
    [Header("Summoning")]
    public GameObject minionPrefab;
    public float summonInterval = 5f;
    public int burstCount = 3;
    public int maxTotalMinions = 9;

    private float summonTimer = 0f;
    private int totalMinionsSummoned = 0;

    public override void OnUpdate()
    {
        if (enemy == null || minionPrefab == null || totalMinionsSummoned >= maxTotalMinions)
            return;

        summonTimer += Time.deltaTime;

        if (summonTimer >= summonInterval)
        {
            SummonBurst();
            summonTimer = 0f;
        }
    }

    private void SummonBurst()
    {
        int remaining = maxTotalMinions - totalMinionsSummoned;
        int toSummon = Mathf.Min(burstCount, remaining);

        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();

        for (int i = 0; i < toSummon; i++)
        {
            float offsetX = Random.Range(1f, 2.5f) * (i % 2 == 0 ? 1 : -1); // alternate left/right
            float offsetY = Random.Range(-1.5f, 1.5f);
            Vector3 spawnPos = enemy.transform.position + new Vector3(offsetX, offsetY, 0f);

            GameObject newMinion = GameObject.Instantiate(minionPrefab, spawnPos, Quaternion.identity);
            Debug.Log($"[BossAbility] Spawned minion at {spawnPos}");

            Enemy minion = newMinion.GetComponent<Enemy>();
            if (minion != null)
            {
                minion.Initialize(10, 2, 1.5f);

                if (spawner != null)
                    spawner.RegisterExternalEnemy(minion);
            }
            else
            {
                Debug.LogWarning("[BossAbility] Minion prefab is missing Enemy component!");
            }

            totalMinionsSummoned++;
        }

        Debug.Log($"[BossAbility] Summoned {toSummon} minion(s). Total: {totalMinionsSummoned}/{maxTotalMinions}");
    }
}
