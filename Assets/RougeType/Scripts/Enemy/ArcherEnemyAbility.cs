using UnityEngine;

public class ArcherEnemyAbility : EnemyAbility
{
    [Header("Archer Settings")]
    // World X where archer stops
    public float stopXPosition = 0f;
    public float shootCooldown = 2f;

    [Header("Projectile")]
    public GameObject arrowPrefab;
    public float arrowSpeed = 6f;
    public int arrowDamage = 1;

    private float lastShootTime;
    private bool hasStopped = false;

    public override void OnUpdate()
    {
        if (enemy == null)
            return;

        // Stop at midpoint
        if (!hasStopped && enemy.transform.position.x <= stopXPosition)
        {
            hasStopped = true;
            enemy.speed = 0f;
        }

        if (!hasStopped)
            return;

        // Shoot at wall
        if (Time.time >= lastShootTime + shootCooldown)
        {
            Shoot();
            lastShootTime = Time.time;
        }
    }

    void Shoot()
    {
        if (arrowPrefab == null)
            return;

        GameObject arrow = Object.Instantiate(
            arrowPrefab,
            enemy.transform.position,
            Quaternion.identity
        );

        ArrowProjectile projectile = arrow.GetComponent<ArrowProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(arrowDamage, arrowSpeed);
        }
    }
}
