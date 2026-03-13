using UnityEngine;

public class SkillTreeUpgradeUIController : MonoBehaviour
{
    public GameObject shopPanel;

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            SkillTreeUpgradeManager.Instance?.RefreshUI();
        }
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }
}

