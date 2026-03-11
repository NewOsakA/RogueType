// Projectile.cs

using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Base")]
    public float speed = 10f;
    public int damage = 1;

    [Header("Chain Shot")]
    public int maxChainCount = 2;
    public float chainRange = 3f;
    public float chainDamageMultiplier = 0.7f;

    [Header("Explosive Shot")]
    public float explosionRadius = 2.5f;
    public float explosionDamageMultiplier = 0.3f;
    public float explosiveRadiusMultiplier = 1f; // to 1.3f

    private Vector3 moveDirection;
    private Transform target;

    private PlayerStats playerStats;
    private TypingManager typingManager;

    private int chainRemaining;
    private Enemy lastEnemyHit;
    private bool burnApplied = false;


    // Init
    public void SetTarget(Transform fallbackTarget)
    {
        playerStats = GameManager.Instance?.playerStats;
        typingManager = Object.FindFirstObjectByType<TypingManager>();

        Enemy priority = Enemy.priorityTargets
            .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
            .FirstOrDefault();

        target = priority != null ? priority.transform : fallbackTarget;

        moveDirection = target != null
            ? (target.position - transform.position).normalized
            : Vector3.right;

        chainRemaining = (playerStats != null && playerStats.hasChainShot)
            ? maxChainCount
            : 0;
    }

    public void SetDamage(int dmg)
    {
        damage = dmg;
    }

    void Start()
    {
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime);
    }

    // Collision
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy")) return;

        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy == null) return;

        int finalDamage = CalculateFinalDamage(enemy);
        enemy.TakeDamage(finalDamage);

        // Burn
        if (!burnApplied && playerStats != null && playerStats.hasBurn)
        {
            burnApplied = true;
            enemy.ApplyBurn(playerStats.burnDamagePerSecond);
        }

        // Explosive Shot
        if (playerStats != null && playerStats.hasExplosiveShot)
        {
            ApplyExplosion(enemy.transform.position, finalDamage);
        }

        // Chain Shot
        if (playerStats != null && playerStats.hasChainShot && chainRemaining > 0)
        {
            ChainToNextEnemy(enemy);
            return; // projectile continues
        }

        Destroy(gameObject);
    }

    // Damage Calculation
    private int CalculateFinalDamage(Enemy enemy)
    {
        int finalDamage = damage;

        if (playerStats == null)
            return finalDamage;

        // Precision Burst
        if (playerStats.hasPrecisionBurst && playerStats.precisionBurstReady)
        {
            finalDamage *= 2;
            playerStats.precisionBurstReady = false;
        }

        // Focused Fire
        if (playerStats.hasFocusedFire)
        {
            finalDamage = Mathf.RoundToInt(
                finalDamage * playerStats.GetFocusedFireMultiplier(enemy)
            );
        }

        // Crit
        if (playerStats.ShouldCrit())
        {
            finalDamage = Mathf.RoundToInt(finalDamage * playerStats.critMultiplier);
        }

        return finalDamage;
    }


    // Chain Shot
    private void ChainToNextEnemy(Enemy current)
    {
        chainRemaining--;

        Enemy next = FindNextEnemy(current);
        if (next == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = current.transform.position;
        target = next.transform;
        moveDirection = (target.position - transform.position).normalized;

        damage = Mathf.RoundToInt(damage * chainDamageMultiplier);
    }

    private Enemy FindNextEnemy(Enemy current)
    {
        return Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)
            .Where(e => e != current)
            .Where(e => Vector3.Distance(current.transform.position, e.transform.position) <= chainRange)
            .OrderBy(e => Vector3.Distance(current.transform.position, e.transform.position))
            .FirstOrDefault();
    }

    // Explosive Shot
    private void ApplyExplosion(Vector3 center, int baseDamage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, GetExplosiveRadius());

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Enemy e = hit.GetComponent<Enemy>();
            if (e == null) continue;

            int aoeDamage = Mathf.RoundToInt(baseDamage * explosionDamageMultiplier);
            e.TakeDamage(aoeDamage);
        }
    }

    public float GetExplosiveRadius()
    {
        float multiplier = 1f;

        if (playerStats != null)
            multiplier = playerStats.explosionRadiusMultiplier;

        return explosionRadius * multiplier;
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chainRange);
    }
#endif
}
