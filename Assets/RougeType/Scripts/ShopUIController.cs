using UnityEngine;

public class ShopUIController : MonoBehaviour
{
    public GameObject shopPanel; // Assign Canvas_shop here

    public void OpenShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(true);
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }
}
