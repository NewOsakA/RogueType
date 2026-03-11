using UnityEngine;

public class ArcherEnemyAbility : EnemyAbility
{
    [Header("Archer Settings")]
    public float stopXPosition = 0f;
    public float shootCooldown = 2f;

    private float lastShootTime;
    private bool hasStopped = false;

    public override void OnUpdate()
    {
        if (enemy == null) return;

        // Stop at midpoint
        if (!hasStopped && enemy.transform.position.x <= stopXPosition)
        {
            hasStopped = true;

            enemy.speed = 0f;
            enemy.SetRunningAnim(false);
        }

        if (!hasStopped) return;

        if (Time.time >= lastShootTime + shootCooldown)
        {
            enemy.PlayAttackAnim();
            lastShootTime = Time.time;
        }
    }
}