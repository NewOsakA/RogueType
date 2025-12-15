using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradeButtonInfo : MonoBehaviour, IPointerEnterHandler
{
    public UpgradeType upgradeType; // Enum: Damage, WallHealth, etc.
    public ShopUpgradeManager upgradeManager;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (upgradeManager != null)
        {
            upgradeManager.OnHover(upgradeType);
        }
    }
}
