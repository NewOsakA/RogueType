// TemporaryUpgradeManager.cs

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


    [Header("Currency")]
    public CurrencyManager currencyManager;
    public TMP_Text currencyText;

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

        CurrencyShow();
    }

    public void CurrencyShow()
    {
        if (currencyManager != null && currencyText != null)
        {
            int currentCurrency = currencyManager.GetCurrentCurrency();
            currencyText.text = $"C: {currentCurrency}";
        }
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

        ApplyUpgrade(data);

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

        CurrencyShow();
    }

    public void ApplyUpgrade(UpgradeData data)
    {
        foreach (var effect in data.effects)
        {
            ApplyEffect(effect);
        }
    }

    // Apply Upgrade Effect
    private void ApplyEffect(UpgradeEffect effect)
    {
        switch (effect.type)
        {
            case UpgradeEffectType.IncreaseDamage:
                playerStats.IncreaseDamage(effect.intValue);
                break;

            case UpgradeEffectType.DecreaseDamage:
                playerStats.IncreaseDamage(-effect.intValue);
                break;

            case UpgradeEffectType.Burn:
                playerStats.hasBurn = true;
                playerStats.burnDamagePerSecond += effect.intValue;
                break;

            case UpgradeEffectType.Execution:
                playerStats.hasExecution = true;
                playerStats.executionThreshold =
                    Mathf.Clamp01(playerStats.executionThreshold + effect.floatValue);
                break;

            case UpgradeEffectType.IncreaseWallHP:
                wall.IncreaseMaxHP(effect.intValue);
                break;

            case UpgradeEffectType.Shield:
                playerStats.shieldHitsPerWave = effect.intValue;
                break;

            case UpgradeEffectType.AutoRepair:
                playerStats.hasAutoRepair = true;
                break;

            case UpgradeEffectType.Fortress:
                playerStats.fortressDamageReduction =
                    Mathf.Clamp01(playerStats.fortressDamageReduction + effect.floatValue);
                break;

            case UpgradeEffectType.ComboDamage:
                playerStats.maxComboBonus += effect.intValue;
                break;

            case UpgradeEffectType.Combo:
                playerStats.comboUpgradeActive = true;
                playerStats.perfectStreakGoal = Mathf.Max(1, effect.intValue);
                break;

            case UpgradeEffectType.CritChance:
                playerStats.critChance += effect.floatValue;
                break;

            case UpgradeEffectType.CritBoost:
                playerStats.critMultiplier = effect.floatValue;
                break;

            case UpgradeEffectType.GoldMultiplier:
                playerStats.goldMultiplier += effect.floatValue;
                break;

            case UpgradeEffectType.Interest:
                playerStats.interestRate =
                    Mathf.Clamp01(playerStats.interestRate + effect.floatValue);
                break;

            case UpgradeEffectType.DiscountShop:
                playerStats.shopDiscount =
                    Mathf.Clamp01(playerStats.shopDiscount + effect.floatValue);
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

            case UpgradeEffectType.AOEBoost:
                playerStats.explosionRadiusMultiplier += effect.floatValue;
                break;

            case UpgradeEffectType.AOEDamage:
                playerStats.explosiveDamageMultiplier += effect.floatValue;
                break;

            case UpgradeEffectType.MultiShot:
                playerStats.projectileCount = Mathf.Max(
                    playerStats.projectileCount,
                    effect.intValue
                );
                break;

            case UpgradeEffectType.MultiShotPenalty:
                playerStats.multiShotDamageMultiplier *= effect.floatValue;
                break;    
        }
    }
}
