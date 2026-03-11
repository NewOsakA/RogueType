using UnityEngine;

public class ArcherShooter : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject arrowPrefab;
    public float arrowSpeed = 6f;
    public int arrowDamage = 1;

    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    public void SpawnArrow()
    {
        if (arrowPrefab == null || enemy == null) return;

        GameObject arrow = Instantiate(
            arrowPrefab,
            enemy.transform.position,
            Quaternion.identity
        );

        var sr = arrow.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.flipX = true;

        ArrowProjectile projectile = arrow.GetComponent<ArrowProjectile>();
        if (projectile != null)
            projectile.Initialize(arrowDamage, arrowSpeed);
    }
}