using UnityEngine;
using TMPro;
using System;

public class EssenceManager : MonoBehaviour
{
    public static EssenceManager Instance;

    [Header("Essence")]
    public int currentEssence = 0;
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
        currentEssence += amount;
        currentEssence = Mathf.Max(0, currentEssence);

        UpdateEssenceText();
        OnEssenceChanged?.Invoke(currentEssence);
    }

    public bool TryConsumeEssence(int amount)
    {
        if (currentEssence < amount)
            return false;

        currentEssence -= amount;
        UpdateEssenceText();
        OnEssenceChanged?.Invoke(currentEssence);
        return true;
    }

    public int GetEssence()
    {
        return currentEssence;
    }

    private void UpdateEssenceText()
    {
        if (essenceText != null)
            essenceText.text = currentEssence.ToString();
    }
}
