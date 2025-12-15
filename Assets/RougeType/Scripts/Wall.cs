using UnityEngine;
using TMPro;

public class Wall : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 100;
    public int currentHP;

    [Header("UI")]
    public TMP_Text hpText;

    private bool shieldAvailable = false;

    void Start()
    {
        currentHP = maxHP;
        UpdateHPDisplay();
    }

    public void TakeDamage(int amount)
    {
        var playerStats = GameManager.Instance?.playerStats;

        if (playerStats != null && playerStats.hasShield && shieldAvailable)
        {
            Debug.Log("Shield blocked wall damage!");
            shieldAvailable = false;
            return;
        }

        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0);
        UpdateHPDisplay();

        Debug.Log($"Wall took {amount} damage => HP: {currentHP}");

        if (currentHP <= 0)
        {
            Debug.Log("Wall destroyed!");

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

        if (playerStats != null && playerStats.hasShield)
        {
            shieldAvailable = true;
            Debug.Log("Shield recharged for this wave.");
        }
        else
        {
            Debug.Log("Shield not recharged — upgrade not unlocked.");
        }
    }

    void UpdateHPDisplay()
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
