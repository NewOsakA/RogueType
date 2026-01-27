// Enemy.cs

using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public static HashSet<Enemy> priorityTargets = new HashSet<Enemy>();

    [Header("Stats")]
    public int maxHP = 10;
    public int currentHP;
    public int damage;
    public float speed = 2f;
    public bool isBoss = false;

    [Header("UI")]
    public TMP_Text hpText;

    [Header("Rewards")]
    public int currencyReward = 10;
    public int essenceReward = 1;

    [Header("Ability")]
    public EnemyAbility specialAbility;

    [Header("Wall Attack")]
    public float attackCooldown = 2f;
    private float lastAttackTime = -Mathf.Infinity;
    private Wall currentWall;

    private TypingManager typingManager;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private Coroutine burnCoroutine;

    public System.Action OnDeath;

    // Init
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        currentHP = maxHP;
        UpdateHPText();

        typingManager = Object.FindFirstObjectByType<TypingManager>();
        typingManager?.RegisterEnemy(this);

        specialAbility?.Initialize(this);

        GameManager.Instance?.RegisterEnemy();
    }

    // Update
    void Update()
    {
        if (currentWall == null)
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime);
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            currentWall.TakeDamage(damage);
            lastAttackTime = Time.time;

            specialAbility?.OnHitWall(currentWall);
        }

        specialAbility?.OnUpdate();
    }

    // Damage
    public void TakeDamage(int amount)
    {
        var stats = GameManager.Instance?.playerStats;

        // Execution (non-boss only)
        if (stats != null && stats.hasExecution && !isBoss)
        {
            float threshold = stats.executionThreshold;

            if (threshold > 0f && currentHP <= Mathf.CeilToInt(maxHP * threshold))
            {
                Die();
                return;
            }
        }

        specialAbility?.OnTakeDamage(ref amount);

        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0);
        UpdateHPText();

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            ResetColor();
        }

        specialAbility?.OnDie();
        typingManager?.UnregisterEnemy(this);

        int reward = currencyReward;
        var stats = GameManager.Instance?.playerStats;
        if (stats != null)
        {
            reward = Mathf.RoundToInt(reward * stats.goldMultiplier);
        }

        if (ExecutionCreditSystem.Instance != null)
        {
            reward = ExecutionCreditSystem.Instance.ApplyBonus(reward);
        }

        if (stats != null && stats.lastFocusedEnemy == this)
        {
            stats.ResetFocusedFire();
        }

        CurrencyManager.Instance?.AddCurrency(reward);

        if (EssenceManager.Instance != null && essenceReward > 0)
        {
            EssenceManager.Instance.AddEssence(essenceReward);
        }
        OnDeath?.Invoke();
        priorityTargets.Remove(this);
        GameManager.Instance?.UnregisterEnemy();
        Destroy(gameObject);
    }

    // UI
    public void UpdateHPText()
    {
        if (hpText != null)
            hpText.text = currentHP.ToString();
    }

    void LateUpdate()
    {
        if (hpText != null)
            hpText.transform.rotation = Quaternion.identity;
    }

    // Wall Collision
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
            currentWall = other.GetComponent<Wall>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Wall") && other.GetComponent<Wall>() == currentWall)
            currentWall = null;
    }

    // INIT From Spawner
    public void Initialize(int hp, int dmg, float spd, bool boss = false)
    {
        maxHP = hp;
        currentHP = hp;
        damage = dmg;
        speed = spd;
        isBoss = boss;
        UpdateHPText();
    }

    // Priority Target
    private void OnMouseDown()
    {
        if (priorityTargets.Contains(this))
        {
            priorityTargets.Remove(this);
            ResetColor();
        }
        else
        {
            priorityTargets.Add(this);
            if (spriteRenderer != null)
                spriteRenderer.color = Color.red;
        }
    }

    // Burn
    public void ApplyBurn(int dps)
    {
        if (burnCoroutine != null)
            StopCoroutine(burnCoroutine);

        burnCoroutine = StartCoroutine(BurnDamageOverTime(dps));
    }

    private IEnumerator BurnDamageOverTime(int dps)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1f, 0.5f, 0.2f);

        const int TICKS = 3;

        for (int i = 0; i < TICKS; i++)
        {
            yield return new WaitForSeconds(1f);
            // Debug.Log($"BURN TICK {i + 1}/{TICKS}");
            TakeDamage(dps);

            if (currentHP <= 0)
                yield break;
        }

        ResetColor();
        burnCoroutine = null;
    }
    private void ResetColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = priorityTargets.Contains(this) ? Color.red : originalColor;
    }

    // Skill apply to enemy
    public void PushBack(float distance)
    {
        transform.position += Vector3.right * distance;
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        StartCoroutine(SlowRoutine(slowPercent, duration));
    }

    IEnumerator SlowRoutine(float slowPercent, float duration)
    {
        float originalSpeed = speed;
        speed *= (1f - slowPercent);

        yield return new WaitForSeconds(duration);

        speed = originalSpeed;
    }
}
