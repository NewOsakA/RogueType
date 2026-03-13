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

    [Header("UI - Health Bar")]
    public HealthBar healthBar;

    private int shieldHitsRemaining = 0;

    [Header("Auto Repair")]
    public int autoRepairAmount = 5;
    public float autoRepairInterval = 5f;
    private bool isDead = false;
    private bool forceOneMaxHp = false;

    void Start()
    {
        currentHP = maxHP;

        if (healthBar == null)
            healthBar = Object.FindFirstObjectByType<HealthBar>(); // fallback

        healthBar?.SetMaxHealth(maxHP);

        UpdateHPDisplay();
    }


    public void TakeDamage(int amount)
    {
        if (isDead) return;

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
            isDead = true;
            GameManager.Instance?.OnPlayerDefeated();
            // gameObject.SetActive(false);
            Die();
        }
    }

    void Die()
    {
        Time.timeScale = 0f; // Pause game

        GameOverUI gameOverUI = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);

        if (gameOverUI != null)
        {
            GameStats stats = GameStats.Instance;
            gameOverUI.Show(
                score: stats != null ? stats.Score : 0,
                totalTime: stats != null ? stats.TotalPlayTime : 0f,
                highestWave: stats != null ? stats.HighestWave : 0,
                currency: stats != null ? stats.CurrentCurrency : 0,
                highestWPM: stats != null ? stats.HighestWPM : 0f,
                averageWPM: stats != null ? stats.AverageWPM : 0f,
                averageAccuracy: stats != null ? stats.AverageAccuracy : 0f,
                worstFingerArea: stats != null ? stats.WorstFingerArea : "N/A"
            );
        }
    }


    public void IncreaseMaxHP(int amount)
    {
        if (forceOneMaxHp)
        {
            maxHP = 1;
            currentHP = 1;
            UpdateHPDisplay();
            return;
        }

        int previousMaxHp = maxHP;
        maxHP = Mathf.Max(1, maxHP + amount);

        int gainedMaxHp = maxHP - previousMaxHp;
        if (gainedMaxHp > 0)
            currentHP += gainedMaxHp;

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        UpdateHPDisplay();
        Debug.Log($"Wall HP upgraded to {maxHP}");
    }

    public void ActivateFragileCannon()
    {
        forceOneMaxHp = true;
        maxHP = 1;
        currentHP = 1;
        UpdateHPDisplay();
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
    public void RepairAfterWaveEnd()
    {
        var playerStats = GameManager.Instance?.playerStats;
        if (playerStats == null || !playerStats.hasAutoRepair)
            return;

        if (currentHP <= 0 || currentHP >= maxHP || autoRepairAmount <= 0)
            return;

        int healAmount = Mathf.Min(autoRepairAmount, maxHP - currentHP);
        Heal(healAmount);
        Debug.Log($"[AutoRepair] Wave end repair +{healAmount} HP → {currentHP}");
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
            hpText.text = $"{currentHP}/{maxHP}";
        }
        if (healthBar != null)
        {
            healthBar.slider.maxValue = maxHP;
            healthBar.SetHealth(currentHP);
        }
    }

    public void ApplyMetaUpgrades()
    {
        var meta = MetaGameManager.Instance;
        if (meta == null) return;

        var modeProfile = meta.GetSelectedDifficultyProfile();
        if (modeProfile != null)
        {
            if (modeProfile.lockWallHpToOne)
            {
                forceOneMaxHp = true;
                maxHP = 1;
                currentHP = 1;
                UpdateHPDisplay();
                return;
            }

            forceOneMaxHp = false;
            maxHP = Mathf.Max(1, modeProfile.wallStartHp);

            if (modeProfile.allowMetaWallUpgrades)
            {
                int hpPerLevelFromMeta = 20;
                maxHP += meta.wallHpLevel * hpPerLevelFromMeta;
            }

            currentHP = maxHP;
            UpdateHPDisplay();
            return;
        }

        int hpPerLevel = 20;
        maxHP += meta.wallHpLevel * hpPerLevel;
        currentHP = maxHP;

        UpdateHPDisplay();
    }
}
