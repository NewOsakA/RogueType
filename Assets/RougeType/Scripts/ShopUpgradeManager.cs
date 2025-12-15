using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum UpgradeType
{
    Damage,
    Damage2,
    Burn,

    WallHealth,
    Shield,
    GoldBoost,

    Combo,
    CritChance,
    CritBoost,
    ComboMaster
}

public class ShopUpgradeManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text descriptionText;
    public TMP_Text costText;

    [Header("Buttons")]
    public Button damageButton;
    public Button damage2Button;
    public Button burnButton;

    public Button wallHealthButton;
    public Button shieldButton;
    public Button goldBoostButton;

    public Button comboButton;
    public Button critChanceButton;
    public Button critBoostButton;
    public Button comboMasterButton;

    [Header("Target")]
    public PlayerStats playerStats;
    public Wall wall;

    [Header("Upgrade Costs")]
    public int damageUpgradeCost = 50;
    public int damage2UpgradeCost = 100;
    public int burnUpgradeCost = 150;

    public int wallHealthUpgradeCost = 50;
    public int shieldUpgradeCost = 100;
    public int goldBoostUpgradeCost = 150;

    public int comboUpgradeCost = 60;
    public int critChanceUpgradeCost = 100;
    public int critBoostUpgradeCost = 120;
    public int comboMasterUpgradeCost = 150;

    void Start()
    {
        damageButton.interactable = true; // Power line start
        wallHealthButton.interactable = true; // Defense line start
        comboButton.interactable = true; // Precision line start

        // All others locked
        damage2Button.interactable = false;
        burnButton.interactable = false;

        shieldButton.interactable = false;
        goldBoostButton.interactable = false;

        critChanceButton.interactable = false;
        critBoostButton.interactable = false;
        comboMasterButton.interactable = false;

        Debug.Log("[ShopUpgradeManager] Upgrade buttons initialized");
    }


    public void OnHover(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Damage:
                UpdateTooltip("Increases your damage by +2.", damageUpgradeCost);
                break;
            case UpgradeType.Damage2:
                UpdateTooltip("Increases your damage by +2 again.", damage2UpgradeCost);
                break;
            case UpgradeType.Burn:
                UpdateTooltip("Enemies take 2 damage/sec for 3s.", burnUpgradeCost);
                break;

            case UpgradeType.WallHealth:
                UpdateTooltip("Increases Wall HP by 50.", wallHealthUpgradeCost);
                break;
            case UpgradeType.Shield:
                UpdateTooltip("Wall blocks 1 hit every wave.", shieldUpgradeCost);
                break;
            case UpgradeType.GoldBoost:
                UpdateTooltip("Earn +50% more currency from kills.", goldBoostUpgradeCost);
                break;

            case UpgradeType.Combo:
                UpdateTooltip("Gain +1 damage every 5 perfect typings (max +3).", comboUpgradeCost);
                break;
            case UpgradeType.CritChance:
                UpdateTooltip("20% chance to deal 2x damage.", critChanceUpgradeCost);
                break;
            case UpgradeType.CritBoost:
                UpdateTooltip("Crit multiplier increases to 3x.", critBoostUpgradeCost);
                break;
            case UpgradeType.ComboMaster:
                UpdateTooltip("Now only 4 perfect typings needed per +1 combo.", comboMasterUpgradeCost);
                break;
        }
    }

    void UpdateTooltip(string description, int cost)
    {
        if (descriptionText != null)
            descriptionText.text = description;

        if (costText != null)
            costText.text = $"Cost: {cost}";
    }

    // LINE A: Power
    public void UpgradeDamage()
    {
        if (CurrencyManager.Instance.SpendCurrency(damageUpgradeCost))
        {
            playerStats.IncreaseDamage(2);
            playerStats.hasDamage1 = true;
            damageButton.interactable = false;

            damage2Button.interactable = true;
        }
    }

    public void UpgradeDamage2()
    {
        if (!playerStats.hasDamage1) return;

        if (CurrencyManager.Instance.SpendCurrency(damage2UpgradeCost))
        {
            playerStats.IncreaseDamage(2);
            playerStats.hasDamage2 = true;
            damage2Button.interactable = false;

            burnButton.interactable = true;
        }
    }

    public void UpgradeBurn()
    {
        if (!playerStats.hasDamage2)
        {
            Debug.LogWarning("[UpgradeBurn] Player does not have Damage2. Upgrade not allowed.");
            return;
        }

        if (CurrencyManager.Instance.SpendCurrency(burnUpgradeCost))
        {
            playerStats.hasBurn = true;
            burnButton.interactable = false;
            Debug.Log("[UpgradeBurn] Burn unlocked! playerStats.hasBurn = true");
        }
        else
        {
            Debug.LogWarning("[UpgradeBurn] Not enough currency for Burn upgrade.");
        }
    }

    // LINE B: Defense
    public void UpgradeWallHealth()
    {
        if (CurrencyManager.Instance.SpendCurrency(wallHealthUpgradeCost))
        {
            wall.IncreaseMaxHP(50);
            playerStats.hasHealth1 = true;
            wallHealthButton.interactable = false;

            shieldButton.interactable = true;
        }
    }

    public void UpgradeShield()
    {
        if (!playerStats.hasHealth1) return;

        if (CurrencyManager.Instance.SpendCurrency(shieldUpgradeCost))
        {
            playerStats.hasShield = true;
            shieldButton.interactable = false;

            goldBoostButton.interactable = true;
        }
    }

    public void UpgradeGoldBoost()
    {
        if (!playerStats.hasShield) return;

        if (CurrencyManager.Instance.SpendCurrency(goldBoostUpgradeCost))
        {
            playerStats.hasGoldBoost = true;
            goldBoostButton.interactable = false;
        }
    }

    // LINE C: Precision
    public void UpgradeCombo()
    {
        if (CurrencyManager.Instance.SpendCurrency(comboUpgradeCost))
        {
            playerStats.comboUpgradeActive = true;
            playerStats.hasCombo = true;
            comboButton.interactable = false;

            critChanceButton.interactable = true;
        }
    }

    public void UpgradeCritChance()
    {
        if (!playerStats.hasCombo) return;

        if (CurrencyManager.Instance.SpendCurrency(critChanceUpgradeCost))
        {
            playerStats.critChance = 0.2f;
            playerStats.hasCritChance = true;
            critChanceButton.interactable = false;

            critBoostButton.interactable = true;
        }
    }

    public void UpgradeCritBoost()
    {
        if (!playerStats.hasCritChance) return;

        if (CurrencyManager.Instance.SpendCurrency(critBoostUpgradeCost))
        {
            playerStats.critMultiplier = 3f;
            playerStats.hasCritBoost = true;
            critBoostButton.interactable = false;

            comboMasterButton.interactable = true;
        }
    }

    public void UpgradeComboMaster()
    {
        if (!playerStats.hasCritBoost) return;

        if (CurrencyManager.Instance.SpendCurrency(comboMasterUpgradeCost))
        {
            playerStats.perfectStreakGoal = 4;
            playerStats.hasComboMaster = true;
            comboMasterButton.interactable = false;
        }
    }
}
