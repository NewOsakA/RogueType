using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    public int currency = 0;
    public TMP_Text currencyText; // 👈 Drag your UI text here

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
        Debug.Log($"Gained {amount} currency! Total: {currency}");
    }

    public bool SpendCurrency(int amount)
    {
        if (currency >= amount)
        {
            currency -= amount;
            UpdateCurrencyUI();
            return true;
        }
        return false;
    }

    void UpdateCurrencyUI()
    {
        if (currencyText != null)
            currencyText.text = $"Currency: {currency}";
    }
}
