using System.Linq;
using UnityEngine;

public enum AllyElement
{
    None,
    Flame,
    Frost
}

[System.Serializable]
public class AllySlot
{
    public string allyName = "Ally";
    public Transform anchor;
    public Vector3 wallOffset;
    public Vector3 visualScale = Vector3.one;
    public Sprite[] idleFrames;
    public float idleFrameRate = 8f;
    public Sprite[] shootFrames;
    public float shootFrameRate = 14f;
    public Sprite arrowSprite;
    public Vector3 projectileSpawnOffset = new Vector3(0.35f, 0.15f, 0f);
    public float arrowVisualSpeed = 8f;
    public string sortingLayerName = "Enemy";
    public int sortingOrderOffset = 25;
    public bool flipVisualX = false;

    [Header("Combat")]
    public int baseDamage = 1;
    public float baseAttackInterval = 1.5f;
    public int burnDamagePerSecond = 1;
    [Range(0f, 1f)] public float slowPercent = 0.3f;
    public float slowDuration = 1.5f;

    [HideInInspector] public bool unlocked;
    [HideInInspector] public int bonusDamage;
    [HideInInspector] public float intervalMultiplier = 1f;
    [HideInInspector] public AllyElement element = AllyElement.None;
    [HideInInspector] public float nextAttackTime;
    [HideInInspector] public GameObject spawnedVisual;
    [HideInInspector] public AllyVisual visual;

    public int CurrentDamage => Mathf.Max(1, baseDamage + bonusDamage);
    public float CurrentAttackInterval => Mathf.Max(0.15f, baseAttackInterval * intervalMultiplier);
}

public class AllyManager : MonoBehaviour
{
    public static AllyManager Instance;

    [Header("Allies")]
    public AllySlot ally1 = new AllySlot
    {
        allyName = "Ally 1",
        baseDamage = 2,
        wallOffset = new Vector3(0.15f, 0.9f, 0f)
    };
    public AllySlot ally2 = new AllySlot
    {
        allyName = "Ally 2",
        baseDamage = 2,
        wallOffset = new Vector3(0.15f, -0.9f, 0f)
    };

    private Wall wall;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        wall = Object.FindFirstObjectByType<Wall>();
        RefreshVisuals();
    }

    void Update()
    {
        if (wall == null)
            wall = Object.FindFirstObjectByType<Wall>();

        UpdateVisual(ally1);
        UpdateVisual(ally2);

        if (GameManager.Instance == null || !GameManager.Instance.IsWavePhase() || GameManager.Instance.isPaused)
            return;

        TryAttack(ally1);
        TryAttack(ally2);
    }

    public void ResetRunStats()
    {
        ResetSlot(ally1);
        ResetSlot(ally2);
        RefreshVisuals();
    }

    public void UnlockAlly(int allyIndex)
    {
        AllySlot slot = GetSlot(allyIndex);
        if (slot == null)
            return;

        slot.unlocked = true;
        slot.nextAttackTime = Time.time;
        RefreshVisuals();
    }

    public void AddDamage(int allyIndex, int amount)
    {
        AllySlot slot = GetSlot(allyIndex);
        if (slot == null)
            return;

        slot.bonusDamage += amount;
    }

    public void ReduceInterval(int allyIndex, float reductionPercent)
    {
        AllySlot slot = GetSlot(allyIndex);
        if (slot == null)
            return;

        float clampedPercent = Mathf.Clamp(reductionPercent, 0f, 0.9f);
        slot.intervalMultiplier *= 1f - clampedPercent;
    }

    public void SetElement(int allyIndex, AllyElement element)
    {
        AllySlot slot = GetSlot(allyIndex);
        if (slot == null)
            return;

        slot.element = element;
    }

    private void ResetSlot(AllySlot slot)
    {
        if (slot == null)
            return;

        DestroyVisual(slot);
        slot.unlocked = false;
        slot.bonusDamage = 0;
        slot.intervalMultiplier = 1f;
        slot.element = AllyElement.None;
        slot.nextAttackTime = 0f;
    }

    private AllySlot GetSlot(int allyIndex)
    {
        return allyIndex switch
        {
            1 => ally1,
            2 => ally2,
            _ => null
        };
    }

    private void TryAttack(AllySlot slot)
    {
        if (slot == null || !slot.unlocked || Time.time < slot.nextAttackTime)
            return;

        Enemy target = FindTarget(slot);
        if (target == null)
            return;

        slot.nextAttackTime = Time.time + slot.CurrentAttackInterval;
        FireAt(slot, target);
    }

    private Enemy FindTarget(AllySlot slot)
    {
        Enemy priorityTarget = Enemy.priorityTargets
            .Where(enemy => enemy != null && enemy.isActiveAndEnabled)
            .OrderBy(enemy => GetDistance(slot, enemy))
            .FirstOrDefault();

        if (priorityTarget != null)
            return priorityTarget;

        return Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)
            .Where(enemy => enemy != null && enemy.isActiveAndEnabled)
            .OrderBy(enemy => enemy.transform.position.x)
            .FirstOrDefault();
    }

    private float GetDistance(AllySlot slot, Enemy enemy)
    {
        if (slot.anchor == null || enemy == null)
            return 0f;

        return Vector3.Distance(slot.anchor.position, enemy.transform.position);
    }

    private void FireAt(AllySlot slot, Enemy target)
    {
        if (slot.spawnedVisual == null || target == null)
            return;

        float dist = Vector3.Distance(
            slot.spawnedVisual.transform.position,
            target.transform.position
        );

        if (dist < 0.6f)
        {
            ApplyHit(slot, target);
            slot.visual?.PlayShoot();
            return;
        }

        if (!PlayShootVisual(slot, target))
            ApplyHit(slot, target);
    }

    private void RefreshVisuals()
    {
        SetVisualState(ally1);
        SetVisualState(ally2);
    }

    private void SetVisualState(AllySlot slot)
    {
        if (slot == null)
            return;

        if (slot.unlocked)
            EnsureVisual(slot);
        else
            DestroyVisual(slot);
    }

    private void UpdateVisual(AllySlot slot)
    {
        if (slot == null || !slot.unlocked || slot.spawnedVisual == null)
            return;

        Transform targetTransform = slot.anchor != null ? slot.anchor : wall != null ? wall.transform : null;
        if (targetTransform == null)
            return;

        slot.spawnedVisual.transform.position = targetTransform.position + slot.wallOffset;
        UpdateSorting(slot);
    }

    private void EnsureVisual(AllySlot slot)
    {
        if (slot.spawnedVisual != null)
            return;

        Transform targetTransform = slot.anchor != null ? slot.anchor : wall != null ? wall.transform : null;
        Vector3 spawnPosition = targetTransform != null
            ? targetTransform.position + slot.wallOffset
            : slot.wallOffset;

        slot.spawnedVisual = new GameObject($"{slot.allyName}_Visual");
        slot.spawnedVisual.transform.position = spawnPosition;
        slot.spawnedVisual.transform.localScale = slot.visualScale;
        slot.spawnedVisual.name = $"{slot.allyName}_Visual";

        PrepareVisualInstance(slot);
        UpdateSorting(slot);
    }

    private void PrepareVisualInstance(AllySlot slot)
    {
        if (slot.spawnedVisual == null)
            return;

        SpriteRenderer renderer = slot.spawnedVisual.AddComponent<SpriteRenderer>();
        slot.visual = slot.spawnedVisual.AddComponent<AllyVisual>();
        slot.visual.spriteRenderer = renderer;
        slot.visual.sortingLayerName = slot.sortingLayerName;
        slot.visual.sortingOrderOffset = slot.sortingOrderOffset;
        slot.visual.flipX = slot.flipVisualX;
        slot.visual.idleFrames = slot.idleFrames;
        slot.visual.idleFrameRate = slot.idleFrameRate;
        slot.visual.shootFrames = slot.shootFrames;
        slot.visual.shootFrameRate = slot.shootFrameRate;
        slot.visual.PlayIdle();
    }

    private void DestroyVisual(AllySlot slot)
    {
        if (slot?.spawnedVisual != null)
            Destroy(slot.spawnedVisual);

        slot.spawnedVisual = null;
        slot.visual = null;
    }

    private void UpdateSorting(AllySlot slot)
    {
        if (slot?.spawnedVisual == null || slot.visual == null)
            return;

        slot.visual.SetSorting(slot.spawnedVisual.transform.position);
    }

    private bool PlayShootVisual(AllySlot slot, Enemy target)
    {
        if (slot == null)
            return false;

        slot.visual?.PlayShoot();

        if (slot.arrowSprite == null || slot.spawnedVisual == null)
            return false;

        GameObject arrowVisual = new GameObject($"{slot.allyName}_Arrow");
        arrowVisual.transform.position = slot.spawnedVisual.transform.position + slot.projectileSpawnOffset;

        SpriteRenderer renderer = arrowVisual.AddComponent<SpriteRenderer>();
        renderer.sprite = slot.arrowSprite;

        AllyArrowVisual arrow = arrowVisual.GetComponent<AllyArrowVisual>();
        if (arrow == null)
            arrow = arrowVisual.AddComponent<AllyArrowVisual>();

        arrow.Initialize(
            target,
            target != null ? target.transform : null,
            slot.arrowVisualSpeed,
            slot.sortingLayerName,
            slot.CurrentDamage,
            slot.element,
            slot.burnDamagePerSecond,
            slot.slowPercent,
            slot.slowDuration,
            renderer,
            slot.flipVisualX
        );

        return true;
    }

    private void ApplyHit(AllySlot slot, Enemy target)
    {
        if (slot == null || target == null)
            return;

        target.TakeDamage(slot.CurrentDamage);

        switch (slot.element)
        {
            case AllyElement.Flame:
                target.ApplyBurn(slot.burnDamagePerSecond);
                break;
            case AllyElement.Frost:
                target.ApplySlow(slot.slowPercent, slot.slowDuration);
                break;
        }
    }
}
