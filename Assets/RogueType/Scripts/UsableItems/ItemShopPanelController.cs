using UnityEngine;

public class ItemShopPanelController : MonoBehaviour
{
    [SerializeField] private GameObject shopPanel;

    void Start()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    public void OpenShop()
    {
        if (!GameManager.Instance.IsBasePhase())
            return;

        shopPanel.SetActive(true);

        foreach (var ui in shopPanel.GetComponentsInChildren<ItemShopUI>())
        {
            ui.Refresh();
        }
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
    }
}
