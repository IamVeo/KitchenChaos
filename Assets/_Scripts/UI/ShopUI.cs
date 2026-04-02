using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currencyText;
    
    private void Start()
    {
        ShopManager.Instance.OnCurrencyChanged += ShopManager_OnCurrencyChanged;
        ShopManager.Instance.OnItemPurchased += ShopManager_OnItemPurchased;
    }
    
    private void ShopManager_OnCurrencyChanged(int newCurrency)
    {
        currencyText.text = $"Coins: {newCurrency}";
    }
    
    private void ShopManager_OnItemPurchased((ShopItemSO item, bool successful) purchase)
    {
        if (purchase.successful)
        {
            Debug.Log($"Purchased {purchase.item.itemName} for {purchase.item.price} coins.");
        }
        else
        {
            Debug.Log($"Failed to purchase {purchase.item.itemName}. Not enough coins.");
        }
    }
}
