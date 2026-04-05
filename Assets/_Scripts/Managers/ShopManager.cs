using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }
    
    [SerializeField] private ShopItemListSO shopItemListSO;

    private List<ShopItemCategory> categories = new();
    private Dictionary<ShopItemCategory, List<ShopItemSO>> shopItemDict = new();
    
    public event Action<int> OnCoinChanged;
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
    }

    private void Start()
    {
        CurrencyManager.Instance.OnCurrencyBalanceChanged += CurrencyManager_OnCurrencyBalanceChanged;
        
        OnCoinChanged?.Invoke(CurrencyManager.Instance.GetBalance(CurrencyType.Coin));
    }

    // ============== PRIVATE METHODS ==============
    
    private void CurrencyManager_OnCurrencyBalanceChanged(CurrencyType type, int newBalance)
    {
        if (type == CurrencyType.Coin)
        {
            OnCoinChanged?.Invoke(newBalance);
        }
    }
    
    private void InitCategories()
    {
        foreach (ShopItemCategory category in Enum.GetValues(typeof(ShopItemCategory)))
        {
            categories.Add(category);
            shopItemDict[category] = new List<ShopItemSO>();
        }
    }

    private void InitShopItems()
    {
        foreach (ShopItemSO item in shopItemListSO.shopItemSOList)
        {
            shopItemDict[item.category].Add(item);
        }
    }
    
    // ============== PUBLIC METHODS ==============
    
    public List<ShopItemCategory> GetCategories()
    {
        return categories;
    }
    
    public List<ShopItemSO> GetShopItemsByCategory(ShopItemCategory category)
    {
        return shopItemDict[category];
    }
    
    public Dictionary<ShopItemCategory, List<ShopItemSO>> GetAllShopItems()
    {
        return shopItemDict;
    }
    
    public ShopItemSO GetShopItemById(string id)
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
            Debug.Log("Purchase successful: " + item.itemName);
            OnCoinChanged?.Invoke(CurrencyManager.Instance.GetBalance(CurrencyType.Coin));
            OnItemPurchased?.Invoke((item, true));
        }
        else
        {
            Debug.Log("ShopManager: Not enough coins to purchase this item: " + item.itemName);
            OnItemPurchased?.Invoke((item, false));
        }
    }
}
