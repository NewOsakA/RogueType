using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class ItemHotbarSlot
{
    public UsableItemType itemType;
    public Image iconImage;
    public TMP_Text countText;
    public TMP_Text keyText;
}

public class ItemHotbarUI : MonoBehaviour
{
    public ItemHotbarSlot[] slots;

    [Header("Visual")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        if (ItemInventory.Instance == null)
            return;

        foreach (var slot in slots)
        {
            int count = ItemInventory.Instance.GetCount(slot.itemType);

            slot.countText.text = $"x{count}";

            slot.iconImage.color = count > 0 ? activeColor : inactiveColor;
        }
    }
}
