using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Damage Stats")]
    public int baseDamage = 1;
    public int bonusDamageFromUpgrades = 0;
    public int currentDamage;
    public int currentComboBonus = 0;

    [Header("Combo System")]
    public bool comboUpgradeActive = false;
    public int comboCount = 0;
    public int maxComboBonus = 3;
    public int perfectStreakGoal = 5;

    [Header("Crit System")]
    public float critChance = 0f;
    public float critMultiplier = 2f;

    [Header("Upgrade Flags")]
    // Power path
    public bool hasDamage1 = false;
    public bool hasDamage2 = false;
    public bool hasBurn = false;

    // Defense path
    public bool hasHealth1 = false;
    public bool hasShield = false;
    public bool hasGoldBoost = false;

    // Precision path
    public bool hasCombo = false;
    public bool hasCritChance = false;
    public bool hasCritBoost = false;
    public bool hasComboMaster = false;

    [Header("Typing Accuracy Tracking")] 
    public int totalCorrect = 0;
    public int totalMistakes = 0;

    void Start()
    {
        ResetStats();
    }

    public void ResetStats()
    {
        bonusDamageFromUpgrades = 0;
        comboCount = 0;
        currentComboBonus = 0;
        currentDamage = baseDamage;
        critChance = 0f;
        critMultiplier = 2f;
        totalCorrect = 0;    
        totalMistakes = 0;
    }

    public void IncreaseDamage(int amount)
    {
        bonusDamageFromUpgrades += amount;
        UpdateCurrentDamage();
        Debug.Log($"[Upgrade] Damage increased by {amount}, Total: {currentDamage}");
    }

    /// <summary>
    /// Called whenever the player types a word. isPerfect = true if no errors were made
    /// </summary>
    /// <param name="isPerfect"></param>
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

    private void UpdateCurrentDamage()
    {
        currentComboBonus = comboUpgradeActive
            ? Mathf.Min(comboCount / perfectStreakGoal, maxComboBonus)
            : 0;

        currentDamage = baseDamage + bonusDamageFromUpgrades + currentComboBonus;

        Debug.Log($"[Damage Calc] base({baseDamage}) + upgrade({bonusDamageFromUpgrades}) + combo({currentComboBonus}) = {currentDamage}");
    }

    public bool ShouldCrit()
    {
        return Random.value < critChance;
    }

    public void ApplyMetaUpgrades()
    {
        var meta = MetaGameManager.Instance;
        if (meta == null) return;

        // Damage Upgrade → เพิ่มความแรง
        bonusDamageFromUpgrades = meta.damageLevel;

        // Combo unlock
        comboUpgradeActive = meta.damageLevel > 0;

        UpdateCurrentDamage();
    }
}
