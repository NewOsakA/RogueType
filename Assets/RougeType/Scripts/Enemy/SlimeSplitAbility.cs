// SlimeSplitAbility.cs

using UnityEngine;

public class SlimeSplitAbility : EnemyAbility
{
    [Header("Split Settings")]
    public GameObject smallSlimePrefab;
    public int splitCount = 2;
    public float spawnOffset = 0.5f;

    [Header("Small Slime Stats")]
    public int smallSlimeHP = 3;
    public int smallSlimeDamage = 1;
    public float smallSlimeSpeed = 3f;

    public override void OnDie()
    {
        if (smallSlimePrefab == null)
            return;

        for (int i = 0; i < splitCount; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-spawnOffset, spawnOffset),
                Random.Range(-spawnOffset, spawnOffset),
                0f
            );

            GameObject slimeObj = Object.Instantiate(
                smallSlimePrefab,
                enemy.transform.position + offset,
                Quaternion.identity
            );

            Enemy slimeEnemy = slimeObj.GetComponent<Enemy>();
            if (slimeEnemy != null)
            {
                slimeEnemy.Initialize(
                    smallSlimeHP,
                    smallSlimeDamage,
                    smallSlimeSpeed,
                    boss: false
                );

                // Register with spawner so wave completion works
                EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
                if (spawner != null)
                {
                    spawner.RegisterExternalEnemy(slimeEnemy);
                }
            }
        }
    }
}
