using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }
    
    [SerializeField] private ShopItemListSO shopItemListSO;

    private List<ShopItemCategory> categories = new();
    private Dictionary<ShopItemCategory, List<ShopItemSO>> shopItems = new();
    
    public event Action<int> OnCurrencyChanged;
    public event Action<(ShopItemSO item, bool successful)> OnItemPurchased;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("Multiple instances of ShopManager detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        InitCategories();
        InitShopItems();
        
        OnCurrencyChanged?.Invoke(CurrencyManager.Instance.GetBalance(CurrencyType.Coin));
    }

    // ============== PRIVATE METHODS ==============
    
    private void InitCategories()
    {
        foreach (ShopItemCategory category in Enum.GetValues(typeof(ShopItemCategory)))
        {
            categories.Add(category);
            shopItems[category] = new List<ShopItemSO>();
        }
    }

    private void InitShopItems()
    {
        foreach (ShopItemSO item in shopItemListSO.shopItemSOList)
        {
            shopItems[item.category].Add(item);
        }
    }
    
    // ============== PUBLIC METHODS ==============
    
    public List<ShopItemCategory> GetCategories()
    {
        return categories;
    }
    
    public List<ShopItemSO> GetShopItemsByCategory(ShopItemCategory category)
    {
        return shopItems[category];
    }
    
    private ShopItemSO GetShopItemById(string id)
    {
        foreach (ShopItemSO item in shopItemListSO.shopItemSOList)
        {
            if (item.id == id)
            {
                return item;
            }
        }
        return null;
    }
    
    public void AttemptPurchase(string itemId)
    {
        ShopItemSO item = GetShopItemById(itemId);
        if (item == null)
        {
            Debug.LogError($"ShopManager: AttemptPurchase failed. Item with id {itemId} not found.");
            OnItemPurchased?.Invoke((null, false));
            return;
        }

        if (CurrencyManager.Instance.TrySpendCurrency(CurrencyType.Coin, item.price))
        {
            OnCurrencyChanged?.Invoke(CurrencyManager.Instance.GetBalance(CurrencyType.Coin));
            OnItemPurchased?.Invoke((item, true));
        }
        else
        {
            Debug.Log("ShopManager: Not enough coins to purchase this item.");
            OnItemPurchased?.Invoke((item, false));
        }
    }
}
