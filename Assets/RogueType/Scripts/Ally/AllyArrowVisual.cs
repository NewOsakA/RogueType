using UnityEngine;

public class AllyArrowVisual : MonoBehaviour
{
    private float speed = 8f;
    private string sortingLayerName = "Enemy";
    private SpriteRenderer[] renderers;
    private Transform target;
    private Enemy targetEnemy;
    private bool initialized;
    private int damage;
    private AllyElement element;
    private int burnDamagePerSecond;
    private float slowPercent;
    private float slowDuration;
    private bool impactApplied;
    private Vector3 startPoint;
    private float travelDuration;
    private float elapsedTime;

    [SerializeField] private float minArcHeight = 0.2f;
    [SerializeField] private float maxArcHeight = 0.5f;
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

        startPoint = transform.position;
        travelDuration = CalculateTravelDuration();
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

        float dt = Time.deltaTime;
        elapsedTime += dt;

        float progress = Mathf.Clamp01(travelDuration <= 0.0001f ? 1f : elapsedTime / travelDuration);
        Vector3 currentPosition = EvaluateArc(progress);
        transform.position = currentPosition;

        Vector3 lookAhead = EvaluateArc(Mathf.Clamp01(progress + 0.02f));
        Vector3 direction = lookAhead - currentPosition;
        if (direction.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        UpdateSorting();

        if (target != null && Vector3.Distance(transform.position, target.position) <= hitRadius)
        {
            transform.position = target.position;
            ApplyImpact();
            Destroy(gameObject);
            return;
        }

        if (progress >= 1f)
        {
            if (target != null)
                transform.position = target.position;

            ApplyImpact();
            Destroy(gameObject);
        }
    }

    private float CalculateTravelDuration()
    {
        if (target == null)
            return 0.25f;

        Vector3 displacement = target.position - transform.position;
        float directDistance = Mathf.Max(0.1f, displacement.magnitude);
        return Mathf.Max(0.18f, directDistance / Mathf.Max(0.1f, speed));
    }

    private Vector3 EvaluateArc(float progress)
    {
        Vector3 endPoint = GetCurrentAimPoint();
        Vector3 linearPoint = Vector3.Lerp(startPoint, endPoint, progress);
        float arcHeight = Mathf.Lerp(
            minArcHeight,
            maxArcHeight,
            Mathf.Clamp01(Vector3.Distance(startPoint, endPoint) / 6f)
        );

        linearPoint.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;
        return linearPoint;
    }

    private Vector3 GetCurrentAimPoint()
    {
        if (targetEnemy != null && targetEnemy.isActiveAndEnabled)
            return targetEnemy.transform.position;

        if (target != null)
            return target.position;

        return startPoint + Vector3.right;
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
