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
    }

    // เพิ่มกลับมา (แก้ ERROR)
    public void AddMetaCoins(int amount)
    {
        metaCoins += amount;
        if (metaCoins < 0) metaCoins = 0;
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
        return true;
    }
}
