using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillShopItemUI : MonoBehaviour
{
    public int skillIndex;

    public Button actionButton;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI buttonText;
    public TextMeshProUGUI essenceText;
    public TextMeshProUGUI levelText;

    ActiveSkillData Skill => SkillManager.Instance.GetSkill(skillIndex);

    void OnEnable()
    {
        Refresh();
        CurrencyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
    }

    void OnDisable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
    }

    void OnCurrencyChanged(int newCurrency)
    {
        Refresh();
    }

    public void Refresh()
    {
        var skill = Skill;
        int money = CurrencyManager.Instance.GetCurrentCurrency();

        nameText.text = skill.skillName;
        descriptionText.text = skill.GetDescription();

        essenceText.text = "Essence : " + skill.essenceCost;
        levelText.text = $"Lv.{skill.currentLevel} / {skill.maxLevel}";

        if (skill.currentLevel >= skill.maxLevel)
        {
            buttonText.text = "MAX";
            costText.text = "-";
            SetDisabled();
            return;
        }

        int cost;
        if (skill.currentLevel == 0)
        {
            buttonText.text = "BUY";
            cost = skill.buyCost;
        }
        else
        {
            buttonText.text = "UPGRADE";
            cost = skill.upgradeCosts[skill.currentLevel - 1];
        }

        costText.text = cost.ToString();

        if (money < cost)
            SetDisabled();
        else
            SetEnabled();
    }

    void SetEnabled()
    {
        actionButton.interactable = true;
        buttonText.color = Color.black;
        costText.color = Color.black;
    }

    void SetDisabled()
    {
        actionButton.interactable = false;
        buttonText.color = Color.gray;
        costText.color = Color.gray;
    }

    public void OnClick()
    {
        var skill = Skill;

        if (skill.currentLevel == 0)
            Buy(skill);
        else
            Upgrade(skill);

        Refresh();
    }

    void Buy(ActiveSkillData skill)
    {
        if (!CurrencyManager.Instance.SpendCurrency(skill.buyCost)) return;
        skill.currentLevel = 1;
    }

    void Upgrade(ActiveSkillData skill)
    {
        int cost = skill.upgradeCosts[skill.currentLevel - 1];
        if (!CurrencyManager.Instance.SpendCurrency(cost)) return;
        skill.currentLevel++;
    }
}