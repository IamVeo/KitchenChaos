using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrencyManager
{
    private static CurrencyManager instance;

    public static CurrencyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new CurrencyManager();
                instance.LoadCurrencyData();
            }

            return instance;
        }
    }
    
    public event Action<CurrencyType, int> OnCurrencyBalanceChanged;

    private Dictionary<CurrencyType, int> balances = new();
    
    private const string CURRENCY_KEY_PREFIX = "Currency_";

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
        OnCurrencyBalanceChanged?.Invoke(type, balances[type]);
        SaveCurrencyData(type);
        
        Debug.Log($"Adding {amount} to {type} currency. Current balance: {GetBalance(type)}");
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
            OnCurrencyBalanceChanged?.Invoke(type, balances[type]);
            SaveCurrencyData(type);
            
            Debug.Log($"Spending {amount} of {type} currency. Current balance: {GetBalance(type)}");
            return true;
        }
        
        Debug.LogWarning($"Insufficient {type} currency. Current balance: {currentBalance}, attempted to spend: {amount}");
        return false;
    }
}
