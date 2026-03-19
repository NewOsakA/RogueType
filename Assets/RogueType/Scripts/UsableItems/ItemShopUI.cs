using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ItemShopUI : MonoBehaviour
{
    [Header("Data")]
    public ItemData itemData;

    [Header("UI")]
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text countText;
    public TMP_Text costText;
    public Button buyButton;

    void Start()
    {
        CurrencyManager.Instance?.AddCurrency(0);
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (itemData == null || ItemInventory.Instance == null || CurrencyManager.Instance == null)
            return;

        // Name
        if (nameText != null)
            nameText.text = itemData.itemName;

        // Description
        if (descriptionText != null)
            descriptionText.text = itemData.description;

        // Count
        int current = ItemInventory.Instance.GetCount(itemData.itemType);
        countText.text = $"{current} / {itemData.maxCarry}";

        // Cost
        costText.text = itemData.cost.ToString();

        // Buy button
        bool isFull = current >= itemData.maxCarry;
        bool hasMoney = CurrencyManager.Instance.GetCurrentCurrency() >= itemData.cost;

        buyButton.interactable = !isFull && hasMoney;

        Debug.Log($"Refresh {itemData.itemName}");
    }

    public void OnBuy()
    {
        if (itemData == null)
            return;

        if (!ItemInventory.Instance.CanAdd(itemData))
            return;

        if (!CurrencyManager.Instance.SpendCurrency(itemData.cost))
            return;

        ItemInventory.Instance.Add(itemData);
        Refresh();
        StartCoroutine(ClearSelectionAtEndOfFrame());
    }

    private IEnumerator ClearSelectionAtEndOfFrame()
    {
        yield return null;

        if (buyButton != null &&
            EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == buyButton.gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
