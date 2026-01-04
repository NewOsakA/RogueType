using UnityEngine;

public class Turret : MonoBehaviour
{
    [Header("Stats")]
    public float attackRange = 5f;
    public float attackSpeed = 1f;
    public int damage = 1;

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

        // Check priority targets
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

        Enemy[] allEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
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
            projScript.SetDamage(damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
