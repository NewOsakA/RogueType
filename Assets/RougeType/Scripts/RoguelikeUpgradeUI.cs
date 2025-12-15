using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class RoguelikeUpgradeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI metaCoinText;
    [SerializeField] private TextMeshProUGUI damageLevelText;
    [SerializeField] private TextMeshProUGUI wallHpLevelText;

    private MetaGameManager meta => MetaGameManager.Instance;

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        metaCoinText.text = $"Coins: {meta.metaCoins}";

        int dmgCost = meta.GetUpgradeCost(meta.damageLevel);
        int hpCost = meta.GetUpgradeCost(meta.wallHpLevel);

        damageLevelText.text = $"Damage Lv {meta.damageLevel} / 20 (Cost: {dmgCost})";
        wallHpLevelText.text = $"Wall HP Lv {meta.wallHpLevel} / 20 (Cost: {hpCost})";
    }

    public void OnUpgradeDamage()
    {
        if (meta.TryUpgrade(ref meta.damageLevel))
            RefreshUI();
    }

    public void OnUpgradeWallHp()
    {
        if (meta.TryUpgrade(ref meta.wallHpLevel))
            RefreshUI();
    }

    public void OnStartRun()
    {
        SceneManager.LoadScene("Game Scene");
    }
}
