using UnityEngine;
using TMPro;

public class TurretBuyPanel : MonoBehaviour
{
    [Header("Turret Info")]
    public GameObject turretPrefab;
    public int cost;

    [Header("UI Elements")]
    public TMP_Text descriptionText;
    public TMP_Text costText;

    private TurretSlot slot;

    // Called after spawning the panel
    public void Init(TurretSlot slotRef)
    {
        slot = slotRef;

        if (descriptionText != null)
            descriptionText.text = "Auto-firing turret";

        if (costText != null)
            costText.text = $"Cost: {cost}";
    }

    // Called when the "Buy" button is pressed
    public void OnBuyClicked()
    {
        if (slot == null) return;

        if (CurrencyManager.Instance.SpendCurrency(cost))
        {
            slot.BuildTurret(turretPrefab);
            slot.ClosePanel(); // Clear reference in slot
            Destroy(gameObject); // Close panel
        }
        else
        {
            Debug.Log("Not enough currency!");
        }
    }

    // Called when the "Close" button is pressed
    public void OnCloseClicked()
    {
        slot?.ClosePanel();
        Destroy(gameObject);
    }
}
