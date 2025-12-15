using UnityEngine;

public class Turret : MonoBehaviour
{
    [Header("Stats")]
    public float attackRange = 5f;
    public float attackSpeed = 1f; // Matches what the panel is using
    public int damage = 1;  // NEW: Damage stat for upgrade

    [Header("References")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    private float fireCooldown = 0f;

    void Update()
    {
        fireCooldown -= Time.deltaTime;

        Enemy closestEnemy = FindClosestEnemy();
        if (closestEnemy != null && fireCooldown <= 0f)
        {
            FireAt(closestEnemy);
            fireCooldown = 1f / attackSpeed;
        }
    }

    Enemy FindClosestEnemy()
    {
        Enemy closest = null;
        float shortestDistance = Mathf.Infinity;

        // Check priority targets first
        foreach (Enemy enemy in Enemy.priorityTargets)
        {
            if (enemy == null) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance <= attackRange && distance < shortestDistance)
            {
                shortestDistance = distance;
                closest = enemy;
            }
        }

        if (closest != null)
            return closest;

        // Fallback: all enemies
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        shortestDistance = Mathf.Infinity;

        foreach (Enemy enemy in allEnemies)
        {
            if (enemy == null) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance <= attackRange && distance < shortestDistance)
            {
                shortestDistance = distance;
                closest = enemy;
            }
        }

        return closest;
    }

    void FireAt(Enemy enemy)
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile projScript = projectile.GetComponent<Projectile>();

        if (projScript != null)
        {
            projScript.SetTarget(enemy.transform);
            projScript.SetDamage(damage); // NEW: Pass upgraded damage
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
