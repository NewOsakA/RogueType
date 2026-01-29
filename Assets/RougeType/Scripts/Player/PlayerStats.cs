using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // BASE DAMAGE
    [Header("Base Damage")]
    public int baseDamage = 1;
    public int bonusDamageFromUpgrades = 0;
    public int currentDamage;

    // COMBO SYSTEM
    [Header("Combo System")]
    public bool comboUpgradeActive = false;
    public int comboCount = 0;
    public int currentComboBonus = 0; 
    public int maxComboBonus = 3;
    public int basePerfectStreakGoal = 5;
    public int perfectStreakGoal;

    // CRIT SYSTEM
    [Header("Critical")]
    [Range(0f, 1f)]
    public float critChance = 0f;
    public float critMultiplier = 2f;

    // CORE MODIFIERS
    [Header("Special Skills")]
    public bool hasChainShot = false;
    public bool hasExplosiveShot = false;
    public bool hasTypingFrenzy = false;
    public bool hasPrecisionBurst = false;
    public bool hasFocusedFire = false;
    public bool hasGlassCannon = false;

    // POWER UPGRADES
    [Header("Power Upgrades")]
    public bool hasBurn = false;
    public int burnDamagePerSecond = 0;
    public bool hasExecution = false;
    [Range(0f, 1f)]
    public float executionThreshold = 0f;

    // DEFENSE UPGRADES
    [Header("Defense Upgrades")]
    public int shieldHitsPerWave = 0;
    public bool hasAutoRepair = false;
    [Range(0f, 1f)]
    public float fortressDamageReduction = 0f;

    // ECONOMY UPGRADES
    [Header("Economy Upgrades")]
    public float goldMultiplier = 1f;
    [Range(0f, 1f)]
    public float interestRate = 0f;
    [Range(0f, 1f)]
    public float shopDiscount = 0f;

    // FOCUSED FIRE
    [Header("Focused Fire")]
    public Enemy lastFocusedEnemy = null;
    public int focusedFireStacks = 0;
    public int maxFocusedFireStacks = 5;

    // Precision Burst
    [Header("Precision Burst")]
    public bool precisionBurstReady = false;

    // TYPING STATS (TRACKING)
    [Header("Typing Stats")]
    public int totalCorrect = 0;
    public int totalMistakes = 0;

    // EXPLOSION RADIUS
    [Header("Explosive Shot")]
    public float explosionRadiusMultiplier = 1f;

    // MULTI SHOT
    [Header("Multi Shot")]
    public int projectileCount = 1;
    [Range(0f, 1f)]
    public float multiShotDamageMultiplier = 1f;



    // INIT
    void Start()
    {
        ResetRunStats();
    }

    // RESET PER RUN
    public void ResetRunStats()
    {
        bonusDamageFromUpgrades = 0;
        comboCount = 0;
        currentComboBonus = 0;
        shopDiscount = 0f;
        interestRate = 0f;
        hasBurn = false;
        burnDamagePerSecond = 0;

        perfectStreakGoal = basePerfectStreakGoal;

        critChance = 0f;
        critMultiplier = 1.5f;

        totalCorrect = 0;
        totalMistakes = 0;

        currentDamage = baseDamage;
        hasExecution = false;
        executionThreshold = 0f;

        explosionRadiusMultiplier = 1f;

        projectileCount = 1;
        multiShotDamageMultiplier = 1f;
    }

    // DAMAGE MANIPULATION
    public void IncreaseDamage(int amount)
    {
        bonusDamageFromUpgrades += amount;
        UpdateCurrentDamage();

        Debug.Log($"Upgrade Damage +{amount} → {currentDamage}");
    }

    // TYPING EVENTS
    public void OnCorrectType(bool isPerfect)
    {
        if (isPerfect)
        {
            totalCorrect++;
            comboCount++;
        }
        else
        {
            totalMistakes++;
            comboCount = 0;
        }

        UpdateCurrentDamage();
    }

    // DAMAGE CALCULATION
    public void UpdateCurrentDamage()
    {
        currentComboBonus = comboUpgradeActive
            ? Mathf.Min(comboCount / perfectStreakGoal, maxComboBonus)
            : 0;

        currentDamage = baseDamage + bonusDamageFromUpgrades + currentComboBonus;

        // Glass Cannon modifier
        if (hasGlassCannon)
        {
            currentDamage = Mathf.RoundToInt(currentDamage * 1.5f);
        }
    }

    public void RecalculateDamage()
    {
        UpdateCurrentDamage();
    }

    // CRIT CHECK
    public bool ShouldCrit()
    {
        return Random.value < critChance;
    }

    // Focused Fire
    public float GetFocusedFireMultiplier(Enemy target)
    {
        if (!hasFocusedFire || target == null)
            return 1f;

        if (lastFocusedEnemy == target)
        {
            focusedFireStacks = Mathf.Min(focusedFireStacks + 1, maxFocusedFireStacks);
        }
        else
        {
            lastFocusedEnemy = target;
            focusedFireStacks = 1;
        }
        // +10% per stack
        return 1f + focusedFireStacks * 0.1f; 
    }

    public void ResetFocusedFire()
    {
        lastFocusedEnemy = null;
        focusedFireStacks = 0;
    }

    // Discount
    public float GetShopDiscountMultiplier()
    {
        return 1f - Mathf.Clamp01(shopDiscount);
    }


    // META UPGRADES (OUTSIDE RUN)
    public void ApplyMetaUpgrades()
    {
        var meta = MetaGameManager.Instance;
        if (meta == null) return;

        bonusDamageFromUpgrades += meta.damageLevel;

        comboUpgradeActive = meta.damageLevel > 0;

        UpdateCurrentDamage();
    }
}
