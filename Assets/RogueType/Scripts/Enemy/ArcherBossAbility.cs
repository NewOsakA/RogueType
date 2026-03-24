using System.Collections;
using UnityEngine;

public class ArcherBossAbility : EnemyAbility
{
    [Header("Movement")]
    public float stopXPosition = 0f;

    [Header("Triple Shot")]
    public GameObject arrowPrefab;
    public float arrowSpeed = 7f;
    public int arrowDamage = 2;
    public float shootCooldown = 1.8f;
    public float spreadAngle = 15f;

    [Header("Summon Archers")]
    public GameObject archerEnemyPrefab;
    public int summonCount = 3;
    public float summonCooldown = 8f;
    public float summonOffsetY = 1.5f;

    private float lastShootTime;
    private float lastSummonTime;
    private bool hasStopped = false;

    public override void OnUpdate()
    {
        if (enemy == null)
            return;

        // Stop at position
        if (!hasStopped && enemy.transform.position.x <= stopXPosition)
        {
            hasStopped = true;
            enemy.speed = 0f;
        }

        if (!hasStopped)
            return;

        // Triple shot
        if (Time.time >= lastShootTime + shootCooldown)
        {
            enemy.StartCoroutine(TripleShot());
            lastShootTime = Time.time;
        }

        // Summon archers
        if (Time.time >= lastSummonTime + summonCooldown)
        {
            enemy.StartCoroutine(SummonArchers());
            lastSummonTime = Time.time;
        }
    }

    IEnumerator TripleShot()
    {
        enemy.PlayAttackAnim();

        yield return new WaitForSeconds(0.28f);
        
        FireArrow(0f);
        FireArrow(spreadAngle);
        FireArrow(-spreadAngle);
    }

    void FireArrow(float angle)
    {
        GameObject arrow = Object.Instantiate(
            arrowPrefab,
            enemy.transform.position,
            Quaternion.Euler(0, 0, angle)
        );

        ArrowProjectile projectile = arrow.GetComponent<ArrowProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(arrowDamage, arrowSpeed);
        }
    }

    IEnumerator SummonArchers()
    {
        for (int i = 0; i < summonCount; i++)
        {
            Vector3 spawnPos = enemy.transform.position;
            spawnPos.y += Random.Range(-summonOffsetY, summonOffsetY);

            GameObject obj = Object.Instantiate(
                archerEnemyPrefab,
                spawnPos,
                Quaternion.identity
            );

            GameManager.Instance.RegisterEnemy();

            yield return new WaitForSeconds(0.3f);
        }
    }
}
