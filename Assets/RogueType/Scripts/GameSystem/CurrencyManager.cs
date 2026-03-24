using System;
using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    public int currency = 0;
    public TMP_Text currencyText;

    public event Action<int> OnCurrencyChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateCurrencyUI();
    }

    public void AddCurrency(int amount)
    {
        currency += amount;
        UpdateCurrencyUI();
        OnCurrencyChanged?.Invoke(currency);
    }

    public bool SpendCurrency(int amount)
    {
        if (currency < amount) return false;

        currency -= amount;
        UpdateCurrencyUI();
        OnCurrencyChanged?.Invoke(currency);
        return true;
    }

    public int GetCurrentCurrency() => currency;

    void UpdateCurrencyUI()
    {
        if (currencyText != null)
            currencyText.text = $"C: {currency}";
    }
}
