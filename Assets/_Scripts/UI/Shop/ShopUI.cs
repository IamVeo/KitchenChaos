using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private CategoryVerticalTabsUI categoryVerticalTabsUI;
    [SerializeField] private ShopSlotScrollViewUI shopSlotScrollViewUI;

    private void OnEnable()
    {
        currencyText.text = $"Coins: {CurrencyManager.Instance.GetBalance(CurrencyType.Coin)}";
    }
    
    private void Start()
    {
        ShopManager.Instance.OnCoinChanged += ShopManager_OnCoinChanged;
        ShopManager.Instance.OnItemPurchased += ShopManager_OnItemPurchased;
        
        categoryVerticalTabsUI.InitCategoryTabs(ShopManager.Instance.GetAllShopItems(), shopSlotScrollViewUI);
        
        categoryVerticalTabsUI.SetSelectedCategory(
            (ShopItemCategory)Enum.GetValues(typeof(ShopItemCategory)).GetValue(0));
        
        shopSlotScrollViewUI.SetScrollViewContent(categoryVerticalTabsUI
            .GetSelectedCategoryButtonUI()
            .GetCategoryScrollViewContent());
    }
    
    private void ShopManager_OnCoinChanged(int newCurrency)
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
    
    public void OnCategoryButtonClicked(CategoryButtonUI categoryButtonUI)
    {
        categoryVerticalTabsUI.SetSelectedCategory(categoryButtonUI.GetButtonCategory());
        shopSlotScrollViewUI.SetScrollViewContent(categoryButtonUI.GetCategoryScrollViewContent());
    }

    public void OnBackButtonClicked()
    {
        gameObject.SetActive(false);
    }
    
    // cheat coin temp method
    public void AddCoins(int amount)
    {
        CurrencyManager.Instance.AddCurrency(CurrencyType.Coin, amount);
    }
}
