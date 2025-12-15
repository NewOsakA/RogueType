using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TurretUpgradePanel : MonoBehaviour
{
    [Header("Stat Display")]
    public TMP_Text damageText;
    public TMP_Text attackSpeedText;
    public TMP_Text descriptionText;
    public TMP_Text costDamageText;
    public TMP_Text costAttackSpeedText;

    [Header("Buttons")]
    public Button upgradeDamageButton;
    public Button upgradeAttackSpeedButton;
    public Button destroyButton;
    public Button closeButton;

    private Turret turret;
    private TurretSlot slot;

    private int damageUpgradeCost = 50;
    private int attackSpeedUpgradeCost = 50;

    public void Init(Turret turretRef, TurretSlot slotRef)
    {
        turret = turretRef;
        slot = slotRef;

        UpdateUI();

        upgradeDamageButton.onClick.AddListener(UpgradeDamage);
        upgradeAttackSpeedButton.onClick.AddListener(UpgradeAttackSpeed);
        destroyButton.onClick.AddListener(DestroyTurret);
        closeButton.onClick.AddListener(ClosePanel);
    }

    void UpdateUI()
    {
        damageText.text = $"Damage: {turret.damage} → {turret.damage + 1}";
        attackSpeedText.text = $"Attack Speed: {turret.attackSpeed:F2} → {(turret.attackSpeed * 1.1f):F2}";

        costDamageText.text = $"Cost: {damageUpgradeCost}";
        costAttackSpeedText.text = $"Cost: {attackSpeedUpgradeCost}";

        descriptionText.text = "Hover an upgrade to see details.";
    }

    public void UpgradeDamage()
    {
        if (CurrencyManager.Instance.SpendCurrency(damageUpgradeCost))
        {
            turret.damage += 1;
            UpdateUI();
        }
    }

    public void UpgradeAttackSpeed()
    {
        if (CurrencyManager.Instance.SpendCurrency(attackSpeedUpgradeCost))
        {
            turret.attackSpeed *= 1.1f;
            UpdateUI();
        }
    }

    public void DestroyTurret()
    {
        slot.DestroyTurret();
        ClosePanel();
    }

    public void ClosePanel()
    {
        slot.ClosePanel();
        Destroy(gameObject);
    }

    // ==== Hover Descriptions ====

    public void OnHoverDamageButton()
    {
        descriptionText.text = $"Increase turret damage.\n" +
                               $"Current: {turret.damage} → {turret.damage + 1}\n" +
                               $"Cost: {damageUpgradeCost}";
    }

    public void OnHoverAttackSpeedButton()
    {
        float upgradedSpeed = turret.attackSpeed * 1.1f;
        descriptionText.text = $"Boost attack speed.\n" +
                               $"Current: {turret.attackSpeed:F2} → {upgradedSpeed:F2}\n" +
                               $"Cost: {attackSpeedUpgradeCost}";
    }

    public void OnHoverExit()
    {
        descriptionText.text = "Hover an upgrade to see details.";
    }
}
