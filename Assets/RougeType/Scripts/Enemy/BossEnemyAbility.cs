// BossEnemyAbility.cs

using UnityEngine;
using System.Collections;

public class BossEnemyAbility : EnemyAbility
{
    [Header("Summoning")]
    public GameObject minionPrefab;
    public float summonCooldown = 10f;
    public int burstCount = 3;
    public int maxTotalMinions = 9;

    [Header("Spawn Offset")]
    public float offsetXMin = 1f;
    public float offsetXMax = 2.5f;
    public float offsetYRange = 1.5f;

    private float lastSummonTime;
    private int totalMinionsSummoned = 0;

    public override void OnUpdate()
    {
        if (enemy == null || minionPrefab == null)
            return;

        if (totalMinionsSummoned >= maxTotalMinions)
            return;

        if (Time.time >= lastSummonTime + summonCooldown)
        {
            enemy.StartCoroutine(SummonBurst());
            lastSummonTime = Time.time;
        }
    }

    IEnumerator SummonBurst()
    {
        int remaining = maxTotalMinions - totalMinionsSummoned;
        int toSummon = Mathf.Min(burstCount, remaining);

        for (int i = 0; i < toSummon; i++)
        {
            Vector3 spawnPos = CalculateSpawnPosition(i);

            GameObject minionObj = Object.Instantiate(
                minionPrefab,
                spawnPos,
                Quaternion.identity
            );

            GameManager.Instance.RegisterEnemy();
            totalMinionsSummoned++;


            yield return new WaitForSeconds(0.3f); // stagger spawn
        }
    }

    private Vector3 CalculateSpawnPosition(int index)
    {
        float offsetX = Random.Range(offsetXMin, offsetXMax);
        offsetX *= (index % 2 == 0) ? 1 : -1; // alternate left/right

        float offsetY = Random.Range(-offsetYRange, offsetYRange);

        return enemy.transform.position + new Vector3(offsetX, offsetY, 0f);
    }
}
