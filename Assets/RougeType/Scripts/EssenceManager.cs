using UnityEngine;
using TMPro;
using System;

public class EssenceManager : MonoBehaviour
{
    public static EssenceManager Instance;

    [Header("Essence")]
    public int essence = 0;
    public TMP_Text essenceText;

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
        UpdateEssenceText();
    }

    public void AddEssence(int amount)
    {
        essence += amount;
        essence = Mathf.Max(0, essence);

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

    public int GetEssence()
    {
        return essence;
    }

    private void UpdateEssenceText()
    {
        if (essenceText != null)
            essenceText.text = $"E: {essence}";
            // essenceText.text = Essence.ToString();
    }
}
