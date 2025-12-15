using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;

    private Vector3 moveDirection;
    private Transform target;
    private PlayerStats playerStats;

    public void SetTarget(Transform fallbackTarget)
    {
        playerStats = GameManager.Instance?.playerStats; // assuming you store it globally

        // Priority targeting
        Enemy priority = Enemy.priorityTargets
            .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
            .FirstOrDefault();

        target = priority != null ? priority.transform : fallbackTarget;
        moveDirection = target != null
            ? (target.position - transform.position).normalized
            : Vector3.right;
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                int finalDamage = damage;

                // 🔥 Apply crit
                if (playerStats != null && playerStats.ShouldCrit())
                {
                    finalDamage = Mathf.RoundToInt(damage * playerStats.critMultiplier);
                    Debug.Log($"💥 CRIT HIT! {finalDamage} damage");
                }

                enemy.TakeDamage(finalDamage);

                // 🔥 Apply burn
                if (playerStats != null && playerStats.hasBurn)
                {
                    enemy.ApplyBurn(2, 3f); // 2 DPS for 3 seconds
                }
            }

            Destroy(gameObject);
        }
    }
}
