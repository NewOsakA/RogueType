using UnityEngine;
using TMPro;
using System;

public class EssenceManager : MonoBehaviour
{
    public static EssenceManager Instance;

    [Header("Essence")]
    public int essence = 0;
    public int maxEssence = 100;
    public TMP_Text essenceText;
    public UnityEngine.UI.Slider essenceBar;

    public event Action<int> OnEssenceChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (essenceBar != null)
        {
            essenceBar.maxValue = maxEssence;
            essenceBar.value = essence;
        }

        UpdateEssenceText();
        OnEssenceChanged?.Invoke(essence);
    }

    public void AddEssence(int amount)
    {
        essence += amount;
        essence = Mathf.Clamp(essence, 0, maxEssence);

        UpdateEssenceText();
        OnEssenceChanged?.Invoke(essence);
    }

    public bool TryConsumeEssence(int amount)
    {
        if (essence < amount)
            return false;

        essence -= amount;
        UpdateEssenceText();
        OnEssenceChanged?.Invoke(essence);
        return true;
    }

    public bool HasEnoughEssence(int amount)
    {
        return essence >= amount;
    }

    public int GetEssence()
    {
        return essence;
    }

    private void UpdateEssenceText()
    {
        if (essenceText != null)
            essenceText.text = $"E: {essence}/{maxEssence}";

        if (essenceBar != null)
            essenceBar.value = essence;
    }
}
