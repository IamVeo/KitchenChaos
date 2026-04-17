using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private CategoryVerticalTabsUI categoryVerticalTabsUI;
    [SerializeField] private ShopSlotScrollViewUI shopSlotScrollViewUI;
    [SerializeField] private Sprite fallbackTopUpIcon;
    [SerializeField] private float networkStateCheckIntervalSeconds = 0.75f;

    private List<TopUpPackageSo> runtimeTopUpPackageList;
    private string coinsTabMessage;
    private Coroutine coinsStateMonitorCoroutine;
    private bool? lastHasNetwork;
    private bool? lastHasToken;
    private bool isRefreshingCoinsUi;

    private void OnEnable()
    {
        currencyText.text = $"Coins: {CurrencyManager.Instance.GetBalance(CurrencyType.Coin)}";
    }
    
    private void Start()
    {
        ShopManager.Instance.OnCoinChanged += ShopManager_OnCoinChanged;
        ShopManager.Instance.OnItemPurchased += ShopManager_OnItemPurchased;

        LoadTopUpPackagesAndInitUi();

        if (coinsStateMonitorCoroutine != null)
        {
            StopCoroutine(coinsStateMonitorCoroutine);
        }

        coinsStateMonitorCoroutine = StartCoroutine(CoinsStateMonitorCoroutine());
    }

    private void OnDestroy()
    {
        if (coinsStateMonitorCoroutine != null)
        {
            StopCoroutine(coinsStateMonitorCoroutine);
            coinsStateMonitorCoroutine = null;
        }

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnCoinChanged -= ShopManager_OnCoinChanged;
            ShopManager.Instance.OnItemPurchased -= ShopManager_OnItemPurchased;
        }
    }

    private void LoadTopUpPackagesAndInitUi()
    {
        if (isRefreshingCoinsUi)
        {
            return;
        }

        isRefreshingCoinsUi = true;

        if (string.IsNullOrEmpty(PaymentManager.Instance.CurrentUserToken))
        {
            runtimeTopUpPackageList = new List<TopUpPackageSo>();
            coinsTabMessage = "Please login to view and purchase coin packages.";
            InitializeCategoryUi();
            isRefreshingCoinsUi = false;
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            runtimeTopUpPackageList = new List<TopUpPackageSo>();
            coinsTabMessage = "Cannot load top-up packages. Please check your network connection and try again.";
            InitializeCategoryUi();
            isRefreshingCoinsUi = false;
            return;
        }

        PaymentManager.Instance.GetTopUpPackages(
            onSuccess: backendPackageList =>
            {
                runtimeTopUpPackageList = ConvertBackendPackages(backendPackageList);
                coinsTabMessage = runtimeTopUpPackageList.Count == 0
                    ? "No top-up packages available right now. Please try again later."
                    : null;
                InitializeCategoryUi();
                isRefreshingCoinsUi = false;
            },
            onError: error =>
            {
                Debug.LogWarning($"[ShopUI] Failed to load top-up packages from backend: {error}");
                runtimeTopUpPackageList = new List<TopUpPackageSo>();
                coinsTabMessage = "Cannot load top-up packages. Please check your network connection and try again.";
                InitializeCategoryUi();
                isRefreshingCoinsUi = false;
            });
    }

    private System.Collections.IEnumerator CoinsStateMonitorCoroutine()
    {
        while (isActiveAndEnabled)
        {
            bool hasNetwork = Application.internetReachability != NetworkReachability.NotReachable;
            bool hasToken = !string.IsNullOrEmpty(PaymentManager.Instance.CurrentUserToken);

            if (!lastHasNetwork.HasValue || !lastHasToken.HasValue
                                         || lastHasNetwork.Value != hasNetwork
                                         || lastHasToken.Value != hasToken)
            {
                lastHasNetwork = hasNetwork;
                lastHasToken = hasToken;
                LoadTopUpPackagesAndInitUi();
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, networkStateCheckIntervalSeconds));
        }
    }

    private void InitializeCategoryUi()
    {
        ShopItemCategory selectedCategory = GetDefaultCategory();
        CategoryButtonUI selectedCategoryButton = categoryVerticalTabsUI.GetSelectedCategoryButtonUI();
        if (selectedCategoryButton != null)
        {
            selectedCategory = selectedCategoryButton.GetButtonCategory();
        }

        if (runtimeTopUpPackageList == null)
        {
            runtimeTopUpPackageList = new List<TopUpPackageSo>();
        }

        Dictionary<ShopItemCategory, RectTransform> categoryContentDict = BuildCategoryContentMap(runtimeTopUpPackageList);
        categoryVerticalTabsUI.InitCategoryTabs(categoryContentDict);

        categoryVerticalTabsUI.SetSelectedCategory(selectedCategory);

        if (categoryVerticalTabsUI.GetSelectedCategoryButtonUI() == null)
        {
            categoryVerticalTabsUI.SetSelectedCategory(GetDefaultCategory());
        }

        CategoryButtonUI selectedCategoryButtonUI = categoryVerticalTabsUI.GetSelectedCategoryButtonUI();
        if (selectedCategoryButtonUI != null)
        {
            shopSlotScrollViewUI.SetScrollViewContent(selectedCategoryButtonUI.GetCategoryScrollViewContent());
        }
    }

    private static ShopItemCategory GetDefaultCategory()
    {
        return (ShopItemCategory)Enum.GetValues(typeof(ShopItemCategory)).GetValue(0);
    }

    public void ShowCoinsNoConnectionMessage()
    {
        runtimeTopUpPackageList = new List<TopUpPackageSo>();
        coinsTabMessage = "Cannot load top-up packages. Please check your network connection and try again.";
        InitializeCategoryUi();
    }

    private Dictionary<ShopItemCategory, RectTransform> BuildCategoryContentMap(List<TopUpPackageSo> topUpPackageList)
    {
        Dictionary<ShopItemCategory, RectTransform> categoryContentDict = new();

        foreach (var (category, shopItemList) in ShopManager.Instance.GetAllShopItems())
        {
            if (category == ShopItemCategory.Coins)
            {
                continue;
            }

            Transform contentTransform = shopSlotScrollViewUI.SetUpContent(shopItemList);
            categoryContentDict[category] = contentTransform.GetComponent<RectTransform>();
        }

        categoryContentDict[ShopItemCategory.Coins] = string.IsNullOrWhiteSpace(coinsTabMessage)
            ? shopSlotScrollViewUI.SetUpCoinContent(topUpPackageList)
            : shopSlotScrollViewUI.SetUpCenteredMessageContent(coinsTabMessage);

        return categoryContentDict;
    }

    private List<TopUpPackageSo> ConvertBackendPackages(List<TopUpPackageResponse> backendPackageList)
    {
        List<TopUpPackageSo> result = new List<TopUpPackageSo>();
        if (backendPackageList == null)
        {
            return result;
        }

        backendPackageList.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

        foreach (TopUpPackageResponse package in backendPackageList)
        {
            if (package == null || string.IsNullOrWhiteSpace(package.id))
            {
                continue;
            }

            TopUpPackageSo runtimePackage = ScriptableObject.CreateInstance<TopUpPackageSo>();
            runtimePackage.id = package.id;
            runtimePackage.displayName = string.IsNullOrWhiteSpace(package.displayName)
                ? package.id
                : package.displayName;
            runtimePackage.coinAmount = package.coinAmount;
            runtimePackage.amountVnd = package.amountVnd;
            runtimePackage.orderInfo = string.IsNullOrWhiteSpace(package.orderInfo)
                ? $"Nap {package.coinAmount} coin"
                : package.orderInfo;
            runtimePackage.icon = fallbackTopUpIcon;

            result.Add(runtimePackage);
        }

        return result;
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
