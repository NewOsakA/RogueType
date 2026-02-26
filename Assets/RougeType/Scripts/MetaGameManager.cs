using UnityEngine;

public class MetaGameManager : MonoBehaviour
{
    public static MetaGameManager Instance { get; private set; }

    [Header("Meta Currency")]
    public int metaCoins = 0;

    [Header("Permanent Upgrades (max 20)")]
    public int damageLevel = 0;
    public int wallHpLevel = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromActiveSlot();
    }

    // เพิ่มกลับมา (แก้ ERROR)
    public void AddMetaCoins(int amount)
    {
        metaCoins += amount;
        if (metaCoins < 0) metaCoins = 0;
        SaveToActiveSlot();
    }

    // โบนัสราคาตามสูตร Hybrid
    public int GetUpgradeCost(int level)
    {
        return Mathf.RoundToInt((10 + level * 5) * Mathf.Pow(1.15f, level));
    }

    public bool TryUpgrade(ref int level)
    {
        if (level >= 20) return false;

        int cost = GetUpgradeCost(level);
        if (metaCoins < cost) return false;

        metaCoins -= cost;
        level++;
        SaveToActiveSlot();
        return true;
    }

    public void LoadFromActiveSlot()
    {
        if (!SaveSlotManager.TryGetActiveSlotIndex(out int slotIndex))
        {
            metaCoins = 0;
            damageLevel = 0;
            wallHpLevel = 0;
            return;
        }

        LoadFromSlot(slotIndex);
    }

    public void LoadFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SaveSlotManager.SlotCount)
            return;

        SaveSlotData slot = SaveSlotManager.GetSlot(slotIndex);
        if (!slot.hasData)
        {
            slot = SaveSlotData.CreateNew(slotIndex);
            SaveSlotManager.SetSlot(slotIndex, slot);
        }

        SaveSlotManager.SetActiveSlotIndex(slotIndex);

        metaCoins = Mathf.Max(0, slot.metaCoins);
        damageLevel = Mathf.Clamp(slot.damageLevel, 0, 20);
        wallHpLevel = Mathf.Clamp(slot.wallHpLevel, 0, 20);
    }

    public void SaveToActiveSlot()
    {
        if (!SaveSlotManager.TryGetActiveSlotIndex(out int slotIndex))
            return;

        SaveSlotData slot = SaveSlotManager.GetSlot(slotIndex);
        if (!slot.hasData)
            slot = SaveSlotData.CreateNew(slotIndex);

        slot.metaCoins = metaCoins;
        slot.damageLevel = damageLevel;
        slot.wallHpLevel = wallHpLevel;
        slot.lastPlayedUtc = System.DateTime.UtcNow.ToString("o");

        SaveSlotManager.SetSlot(slotIndex, slot);
    }
}
