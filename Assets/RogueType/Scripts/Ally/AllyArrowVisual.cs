using UnityEngine;

public class AllyArrowVisual : MonoBehaviour
{
    private float speed = 8f;
    private string sortingLayerName = "Enemy";

    private SpriteRenderer[] renderers;

    private Transform target;
    private Enemy targetEnemy;

    private bool initialized;
    private bool impactApplied;

    private int damage;
    private AllyElement element;
    private int burnDamagePerSecond;
    private float slowPercent;
    private float slowDuration;

    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private float hitRadius = 0.28f;

    public void Initialize(
        Enemy enemyTarget,
        Transform targetTransform,
        float arrowSpeed,
        string layerName,
        int hitDamage,
        AllyElement allyElement,
        int burnDps,
        float slowAmount,
        float slowTime,
        SpriteRenderer primaryRenderer = null,
        bool flipX = false
    )
    {
        targetEnemy = enemyTarget;
        target = targetTransform;

        speed = arrowSpeed;
        sortingLayerName = string.IsNullOrWhiteSpace(layerName) ? "Enemy" : layerName;

        damage = hitDamage;
        element = allyElement;
        burnDamagePerSecond = burnDps;
        slowPercent = slowAmount;
        slowDuration = slowTime;

        if (primaryRenderer != null)
            primaryRenderer.flipX = flipX;

        initialized = true;
    }

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    void Start()
    {
        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        if (!initialized)
            return;

        Vector3 targetPos = GetCurrentAimPoint();

        Vector3 direction = (targetPos - transform.position);

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        transform.position += direction * speed * Time.deltaTime;

        // Rotate arrow to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        UpdateSorting();

        // Hit detection
        if (Vector3.Distance(transform.position, targetPos) <= hitRadius)
        {
            transform.position = targetPos;
            ApplyImpact();
            Destroy(gameObject);
        }
    }

    private Vector3 GetCurrentAimPoint()
    {
        if (targetEnemy != null && targetEnemy.isActiveAndEnabled)
            return targetEnemy.transform.position;

        if (target != null)
            return target.position;

        return transform.position + Vector3.right;
    }

    private void ApplyImpact()
    {
        if (impactApplied || targetEnemy == null)
            return;

        impactApplied = true;

        targetEnemy.TakeDamage(damage);

        switch (element)
        {
            case AllyElement.Flame:
                targetEnemy.ApplyBurn(burnDamagePerSecond);
                break;

            case AllyElement.Frost:
                targetEnemy.ApplySlow(slowPercent, slowDuration);
                break;
        }
    }

    private void UpdateSorting()
    {
        if (renderers == null)
            return;

        int order = 5000 - Mathf.RoundToInt(transform.position.y * 100) + 10;

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = order;
        }
    }
}