using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class TemporaryUpgradeManager : MonoBehaviour
{
    public static TemporaryUpgradeManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // UI
    [Header("UI")]
    public TMP_Text upgradeNameText;
    public TMP_Text descriptionText;
    public TMP_Text costText;

    // Targets
    [Header("Targets")]
    public PlayerStats playerStats;
    public Wall wall;

    // Runtime State
    private HashSet<UpgradeData> unlockedUpgrades = new HashSet<UpgradeData>();
    private HashSet<ExclusiveGroup> takenExclusiveGroups = new HashSet<ExclusiveGroup>();

    private UpgradeNode[] nodes;

    // Init
    void Start()
    {
        nodes = Object.FindObjectsByType<UpgradeNode>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
    }

    // UI
    public void ShowInfo(UpgradeData data)
    {
        if (data == null) return;

        if (upgradeNameText != null)
            upgradeNameText.text = data.upgradeName;

        int cost = data.cost;

        if (playerStats != null)
        {
            cost = Mathf.RoundToInt(cost * playerStats.GetShopDiscountMultiplier());
        }

        descriptionText.text = data.description;
        costText.text = $"Cost: {cost}";
    }
    
    public void RefreshUI()
    {
        nodes = Object.FindObjectsByType<UpgradeNode>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        RefreshAll();
    }


    // State Check
    public bool IsUnlocked(UpgradeData data)
    {
        return unlockedUpgrades.Contains(data);
    }

    public bool IsExclusiveGroupTaken(ExclusiveGroup group)
    {
        return takenExclusiveGroups.Contains(group);
    }
    public bool IsGroupLockedFor(UpgradeData data)
    {
        if (data == null) return false;
        if (!data.exclusive) return false;
        if (data.exclusiveGroup == ExclusiveGroup.None) return false;

        return takenExclusiveGroups.Contains(data.exclusiveGroup)
            && !IsUnlocked(data);
    }

    public bool IsPrerequisiteMet(UpgradeData data)
    {
        if (data == null)
            return true;

        if (data.prerequisites == null || data.prerequisites.Count == 0)
            return true;

        if (data.prerequisiteMode == PrerequisiteMode.All)
        {
            foreach (var pre in data.prerequisites)
            {
                if (!IsUnlocked(pre))
                    return false;
            }
            return true;
        }
        else
        {
            foreach (var pre in data.prerequisites)
            {
                if (IsUnlocked(pre))
                    return true;
            }
            return false;
        }
    }

    // Purchase
    public void TryBuy(UpgradeNode node)
    {
        UpgradeData data = node.data;
        if (data == null) return;

        Debug.Log($"[TRY BUY] {data.upgradeName}");

        if (IsUnlocked(data))
        {
            Debug.Log("Already unlocked");
            return;
        }

        if (!IsPrerequisiteMet(data))
        {
            Debug.Log("Prerequisite not met");
            return;
        }

        if (data.exclusive && IsExclusiveGroupTaken(data.exclusiveGroup))
        {
            Debug.Log("Exclusive group taken");
            return;
        }

        int finalCost = data.cost;
        if (!CurrencyManager.Instance.SpendCurrency(finalCost))
        {
            Debug.Log("Not enough money");
            return;
        }

        ApplyEffect(data);

        // Save State
        unlockedUpgrades.Add(data);
        // Debug.Log($"UNLOCKED: {data.upgradeName}");

        if (data.exclusive)
        {
            takenExclusiveGroups.Add(data.exclusiveGroup);
            // Debug.Log($"EXCLUSIVE GROUP TAKEN: {data.exclusiveGroup}");
        }

        // Then refresh all other nodes
        RefreshAll();
    }

    // Refresh Buttons
    private void RefreshAll()
    {
        foreach (var node in nodes)
        {
            if (node != null)
                node.Refresh();
        }
    }

    // Apply Upgrade Effect
    private void ApplyEffect(UpgradeData data)
    {
        switch (data.effectType)
        {
            case UpgradeEffectType.IncreaseDamage:
                playerStats.IncreaseDamage(data.intValue);
                break;

            case UpgradeEffectType.Burn:
                playerStats.hasBurn = true;
                playerStats.burnDamagePerSecond += data.intValue;
                break;

            case UpgradeEffectType.Execution:
                playerStats.hasExecution = true;
                playerStats.executionThreshold =
                    Mathf.Clamp01(playerStats.executionThreshold + data.floatValue);
                break;

            case UpgradeEffectType.IncreaseWallHP:
                wall.IncreaseMaxHP(data.intValue);
                break;

            case UpgradeEffectType.Shield:
                playerStats.shieldHitsPerWave = data.intValue;
                break;

            case UpgradeEffectType.AutoRepair:
                playerStats.hasAutoRepair = true;
                break;

            case UpgradeEffectType.Fortress:
                playerStats.fortressDamageReduction =
                    Mathf.Clamp01(playerStats.fortressDamageReduction + data.floatValue);
                break;

            case UpgradeEffectType.ComboDamage:
                playerStats.maxComboBonus += data.intValue;
                break;

            case UpgradeEffectType.Combo:
                playerStats.comboUpgradeActive = true;
                playerStats.perfectStreakGoal = Mathf.Max(1, data.intValue);
                break;

            case UpgradeEffectType.CritChance:
                playerStats.critChance += data.floatValue;
                break;

            case UpgradeEffectType.CritBoost:
                playerStats.critMultiplier = data.floatValue;
                break;

            case UpgradeEffectType.GoldMultiplier:
                playerStats.goldMultiplier += data.floatValue;
                break;

            case UpgradeEffectType.Interest:
                playerStats.interestRate =
                    Mathf.Clamp01(playerStats.interestRate + data.floatValue);
                break;

            case UpgradeEffectType.DiscountShop:
                playerStats.shopDiscount =
                    Mathf.Clamp01(playerStats.shopDiscount + data.floatValue);
                break;

            case UpgradeEffectType.ChainShot:
                playerStats.hasChainShot = true;
                break;

            case UpgradeEffectType.ExplosiveShot:
                playerStats.hasExplosiveShot = true;
                break;

            case UpgradeEffectType.TypingFrenzy:
                playerStats.hasTypingFrenzy = true;
                break;

            case UpgradeEffectType.PrecisionBurst:
                playerStats.hasPrecisionBurst = true;
                break;

            case UpgradeEffectType.FocusedFire:
                playerStats.hasFocusedFire = true;
                break;

            case UpgradeEffectType.GlassCannon:
                playerStats.hasGlassCannon = true;

                wall.maxHP = Mathf.RoundToInt(wall.maxHP * 0.3f);
                wall.currentHP = Mathf.Min(wall.currentHP, wall.maxHP);
                wall.UpdateHPDisplay();

                playerStats.IncreaseDamage(0);
                break;
        }
    }
}
