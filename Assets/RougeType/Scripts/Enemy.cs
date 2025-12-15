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
    public int currentHP;
    public int damage;
    public float speed = 2f;

    [Header("UI")]
    public TMP_Text hpText;

    [Header("Rewards")]
    public int currencyReward = 10;

    [Header("Ability")]
    public EnemyAbility specialAbility;

    private TypingManager typingManager;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    [Header("Wall Attack")]
    public float attackCooldown = 2f;
    private float lastAttackTime = -Mathf.Infinity;
    private Wall currentWall = null;

    // 🔥 Burn State
    private Coroutine burnCoroutine;
    private bool isBurning = false;

    public System.Action OnDeath;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        UpdateHPText();

        typingManager = FindObjectOfType<TypingManager>();
        typingManager?.RegisterEnemy(this);

        specialAbility?.Initialize(this);
    }

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
            Debug.Log($"🧱 {name} hit wall for {damage} damage");
            specialAbility?.OnHitWall(currentWall);
        }

        specialAbility?.OnUpdate();
    }

    public void TakeDamage(int amount)
    {
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
        if (GameManager.Instance.playerStats != null && GameManager.Instance.playerStats.hasGoldBoost)
        {
            reward = Mathf.RoundToInt(reward * 1.5f); // +50%
            Debug.Log($"💰 Gold Boost! Extra reward: {reward}");
        }

        CurrencyManager.Instance?.AddCurrency(reward);

        OnDeath?.Invoke();
        priorityTargets.Remove(this);
        Destroy(gameObject);
    }

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            currentWall = other.GetComponent<Wall>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Wall") && currentWall != null && other.GetComponent<Wall>() == currentWall)
        {
            currentWall = null;
        }
    }

    public void Initialize(int hp, int dmg, float spd)
    {
        currentHP = hp;
        damage = dmg;
        speed = spd;
        UpdateHPText();
    }

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

    // 🔥 Burn logic
    public void ApplyBurn(int dps, float duration)
    {
        if (isBurning)
        {
            Debug.Log($"🔥 {name} is already burning. Burn skipped.");
            return;
        }

        Debug.Log($"🔥 Burn applied to {name} for {duration}s at {dps} DPS");

        if (burnCoroutine != null)
            StopCoroutine(burnCoroutine);

        burnCoroutine = StartCoroutine(BurnCoroutine(dps, duration));
    }

    private IEnumerator BurnCoroutine(int dps, float duration)
    {
        isBurning = true;

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1f, 0.5f, 0.2f); // 🔥 Orange

        float elapsed = 0f;

        while (elapsed < duration)
        {
            Debug.Log($"🔥 {name} burn tick: -{dps} HP at {elapsed:0.0}s");
            TakeDamage(dps);

            if (currentHP <= 0)
            {
                Debug.Log($"🔥 {name} died from burn.");
                yield break;
            }

            yield return new WaitForSeconds(1f);
            elapsed += 1f;
        }

        Debug.Log($"🔥 Burn finished on {name}");

        ResetColor();
        isBurning = false;
    }

    private void ResetColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = priorityTargets.Contains(this) ? Color.red : originalColor;
    }
}
