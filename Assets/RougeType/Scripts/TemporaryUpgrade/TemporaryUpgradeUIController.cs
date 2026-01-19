using UnityEngine;

public class TemporaryUpgradeUIController : MonoBehaviour
{
    public GameObject shopPanel;

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            TemporaryUpgradeManager.Instance?.RefreshUI();
        }
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }
}

