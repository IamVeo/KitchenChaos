using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    private Dictionary<CurrencyType, int> balances = new();
    
    private const string CURRENCY_KEY_PREFIX = "Currency_";

    public event Action<(CurrencyType type, int amount, bool successful)> OnCurrencyUpdated;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCurrencyData();
        }
        else
        {
            Debug.LogWarning("Multiple instances of CurrencyManager detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    // ================ PRIVATE METHODS ================
    
    private void LoadCurrencyData()
    {
        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
        {
            balances[type] = PlayerPrefs.GetInt($"{CURRENCY_KEY_PREFIX}{type.ToString()}", 0);
        }
    }

    private void SaveCurrencyData(CurrencyType type)
    {
        PlayerPrefs.SetInt($"{CURRENCY_KEY_PREFIX}{type.ToString()}", balances[type]);
        PlayerPrefs.Save();
    }

    private void UpdateCurrencyData(CurrencyType type)
    {
        OnCurrencyUpdated?.Invoke((type, balances[type], true));
        SaveCurrencyData(type);
    }
    
    // ================ PUBLIC METHODS ================
    
    public int GetBalance(CurrencyType type)
    {
        return balances.TryGetValue(type, out int balance) ? balance : 0;
    }

    public void AddCurrency(CurrencyType type, int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"Added amount for {type} currency must be positive. Given: {amount}");
            return;
        }

        balances[type] = GetBalance(type) + amount;
        UpdateCurrencyData(type);
    }

    public bool TrySpendCurrency(CurrencyType type, int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"Spent amount for {type} currency must be positive. Given: {amount}");
            return false;
        }

        int currentBalance = GetBalance(type);
        if (currentBalance >= amount)
        {
            balances[type] = currentBalance - amount;
            UpdateCurrencyData(type);
            
            return true;
        }
        
        Debug.LogWarning($"Insufficient {type} currency. Current balance: {currentBalance}, attempted to spend: {amount}");
        OnCurrencyUpdated?.Invoke((type, currentBalance, false));
        return false;
    }
}
