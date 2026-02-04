using UnityEngine;
using TMPro;
using System.Collections;

public class Wall : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 100;
    public int currentHP;

    [Header("UI")]
    public TMP_Text hpText;

    private int shieldHitsRemaining = 0;

    [Header("Auto Repair")]
    public int autoRepairAmount = 1; 
    public float autoRepairInterval = 5f;

    private Coroutine autoRepairCoroutine;


    void Start()
    {
        currentHP = maxHP;
        UpdateHPDisplay();
    }


public void TakeDamage(int amount)
{
    var playerStats = GameManager.Instance?.playerStats;

    // Shield block
    if (playerStats != null && shieldHitsRemaining > 0)
    {
        shieldHitsRemaining--;
        // Debug.Log($"Shield Remaining: {shieldHitsRemaining}");
        return;
    }

    // Fortress % reduction
    float reduction = 0f;
    if (playerStats != null)
    {
        reduction = playerStats.fortressDamageReduction;
    }

    int finalDamage = Mathf.RoundToInt(amount * (1f - reduction));
    finalDamage = Mathf.Max(finalDamage, 0);

    currentHP -= finalDamage;
    currentHP = Mathf.Max(currentHP, 0);
    UpdateHPDisplay();

    // Debug.Log($"Wall took {finalDamage} damage (reduced from {amount})");

    if (currentHP <= 0)
    {
        GameManager.Instance?.OnPlayerDefeated();
        gameObject.SetActive(false);
    }
}

    public void IncreaseMaxHP(int amount)
    {
        maxHP += amount;
        currentHP = maxHP;
        UpdateHPDisplay();
        Debug.Log($"Wall HP upgraded to {maxHP}");
    }

    public void RechargeShield()
    {
        var playerStats = GameManager.Instance?.playerStats;
        if (playerStats == null) return;

        shieldHitsRemaining = playerStats.shieldHitsPerWave;

        if (shieldHitsRemaining > 0)
        {
            // Debug.Log($"Shield recharged: {shieldHitsRemaining} hits");
        }
    }


    public void StartAutoRepair()
    {
        if (autoRepairCoroutine != null)
            StopCoroutine(autoRepairCoroutine);

        autoRepairCoroutine = StartCoroutine(AutoRepairDuringWave());
    }

    public void StopAutoRepair()
    {
        if (autoRepairCoroutine != null)
        {
            StopCoroutine(autoRepairCoroutine);
            autoRepairCoroutine = null;
        }
    }

    IEnumerator AutoRepairDuringWave()
    {
        while (GameManager.Instance != null &&
            GameManager.Instance.IsWavePhase() &&
            currentHP > 0)
        {
            var playerStats = GameManager.Instance.playerStats;

            if (playerStats != null && playerStats.hasAutoRepair)
            {
                if (currentHP < maxHP)
                {
                    currentHP += autoRepairAmount;
                    currentHP = Mathf.Min(currentHP, maxHP);
                    UpdateHPDisplay();

                    Debug.Log($"[AutoRepair] Wall +{autoRepairAmount} HP → {currentHP}");
                }
            }

            yield return new WaitForSeconds(autoRepairInterval);
        }
    }

    // Items
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        UpdateHPDisplay();

        // Debug.Log($"Wall healed + {amount} Now {currentHP}");
    }

    public void ActivateDamageReduction(float percent, float duration)
    {
        StartCoroutine(DamageReductionRoutine(percent, duration));
    }

    IEnumerator DamageReductionRoutine(float percent, float duration)
    {
        var playerStats = GameManager.Instance?.playerStats;
        if (playerStats == null) yield break;

        float originalReduction = playerStats.fortressDamageReduction;
        playerStats.fortressDamageReduction += percent;

        Debug.Log($"Damage reduction {percent * 100}%");

        yield return new WaitForSeconds(duration);

        playerStats.fortressDamageReduction = originalReduction;
        Debug.Log("Damage reduction ended");
    }

    public void UpdateHPDisplay()
    {
        if (hpText != null)
        {
            hpText.text = $"Wall HP: {currentHP}";
        }
    }

    public void ApplyMetaUpgrades()
    {
        var meta = MetaGameManager.Instance;
        if (meta == null) return;

        int hpPerLevel = 20;
        maxHP += meta.wallHpLevel * hpPerLevel;
        currentHP = maxHP;

        UpdateHPDisplay();
    }
}
