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
    private bool hasPendingCategoryRebuild;

    private void OnEnable()
    {
        if (!ValidateUiDependencies())
        {
            return;
        }

        currencyText.text = $"Coins: {CurrencyManager.Instance.GetBalance(CurrencyType.Coin)}";

        StartCoinsStateMonitor();
        CheckStateAndRefreshCoinsUi(forceRefresh: true);

        if (hasPendingCategoryRebuild)
        {
            InitializeCategoryUi();
        }

        NotifyMainMenuGreeting(false);
    }
    
    private void Start()
    {
        if (ShopManager.Instance == null)
        {
            Debug.LogError("[ShopUI] ShopManager.Instance is null. Please ensure a ShopManager exists in the scene before opening Shop UI.");
            return;
        }

        ShopManager.Instance.OnCoinChanged += ShopManager_OnCoinChanged;
        ShopManager.Instance.OnItemPurchased += ShopManager_OnItemPurchased;
    }

    private void OnDisable()
    {
        StopCoinsStateMonitor();
        NotifyMainMenuGreeting(true);
    }

    private void OnDestroy()
    {
        StopCoinsStateMonitor();

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
            CheckStateAndRefreshCoinsUi(forceRefresh: false);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, networkStateCheckIntervalSeconds));
        }
    }

    private void StartCoinsStateMonitor()
    {
        if (coinsStateMonitorCoroutine != null)
        {
            return;
        }

        coinsStateMonitorCoroutine = StartCoroutine(CoinsStateMonitorCoroutine());
    }

    private void StopCoinsStateMonitor()
    {
        if (coinsStateMonitorCoroutine == null)
        {
            return;
        }

        StopCoroutine(coinsStateMonitorCoroutine);
        coinsStateMonitorCoroutine = null;
    }

    private void CheckStateAndRefreshCoinsUi(bool forceRefresh)
    {
        bool hasNetwork = Application.internetReachability != NetworkReachability.NotReachable;
        bool hasToken = !string.IsNullOrEmpty(PaymentManager.Instance.CurrentUserToken);

        bool stateChanged = !lastHasNetwork.HasValue || !lastHasToken.HasValue
            || lastHasNetwork.Value != hasNetwork
            || lastHasToken.Value != hasToken;

        if (!forceRefresh && !stateChanged)
        {
            return;
        }

        // Do not update the tracked state while a request is still running;
        // keep change detection active so the next tick can refresh immediately.
        if (isRefreshingCoinsUi)
        {
            return;
        }

        lastHasNetwork = hasNetwork;
        lastHasToken = hasToken;
        LoadTopUpPackagesAndInitUi();
    }


    private void InitializeCategoryUi()
    {
        if (!CanRebuildCategoryUiNow())
        {
            hasPendingCategoryRebuild = true;
            return;
        }

        if (!ValidateUiDependencies())
        {
            return;
        }

        if (ShopManager.Instance == null)
        {
            Debug.LogError("[ShopUI] Cannot initialize category UI because ShopManager.Instance is null.");
            return;
        }

        hasPendingCategoryRebuild = false;

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

        // Remove stale content roots from previous rebuilds before creating new category contents.
        shopSlotScrollViewUI.ClearGeneratedContents();

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
            RectTransform selectedContent = selectedCategoryButtonUI.GetCategoryScrollViewContent();
            if (selectedContent != null)
            {
                shopSlotScrollViewUI.SetScrollViewContent(selectedContent);
            }
            else
            {
                Debug.LogWarning("[ShopUI] Selected category content is null. Check ShopSlotScrollViewUI prefab references.");
            }
        }
    }

    private bool CanRebuildCategoryUiNow()
    {
        return isActiveAndEnabled && gameObject.activeInHierarchy;
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
            if (contentTransform == null)
            {
                Debug.LogWarning($"[ShopUI] Failed to create content root for category '{category}'.");
                continue;
            }

            RectTransform contentRect = contentTransform.GetComponent<RectTransform>();
            if (contentRect == null)
            {
                Debug.LogWarning($"[ShopUI] Missing RectTransform on generated content for category '{category}'.");
                continue;
            }

            categoryContentDict[category] = contentRect;
        }

        RectTransform coinsContent = string.IsNullOrWhiteSpace(coinsTabMessage)
            ? shopSlotScrollViewUI.SetUpCoinContent(topUpPackageList)
            : shopSlotScrollViewUI.SetUpCenteredMessageContent(coinsTabMessage);

        if (coinsContent != null)
        {
            categoryContentDict[ShopItemCategory.Coins] = coinsContent;
        }
        else
        {
            Debug.LogWarning("[ShopUI] Failed to create Coins tab content.");
        }

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
        if (categoryButtonUI == null)
        {
            return;
        }

        if (!ValidateUiDependencies())
        {
            return;
        }

        categoryVerticalTabsUI.SetSelectedCategory(categoryButtonUI.GetButtonCategory());

        RectTransform contentRectTransform = categoryButtonUI.GetCategoryScrollViewContent();
        if (contentRectTransform != null)
        {
            shopSlotScrollViewUI.SetScrollViewContent(contentRectTransform);
        }
    }

    private bool ValidateUiDependencies()
    {
        if (currencyText == null || categoryVerticalTabsUI == null || shopSlotScrollViewUI == null)
        {
            Debug.LogError("[ShopUI] Missing serialized references. Re-open ShopUI prefab and rebind Currency Text / CategoryVerticalTabsUI / ShopSlotScrollViewUI.");
            return false;
        }

        return true;
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

    private void NotifyMainMenuGreeting(bool isVisible)
    {
        MainMenuUI mainMenu = FindObjectOfType<MainMenuUI>();
        if (mainMenu != null)
        {
            mainMenu.SetGreetingVisible(isVisible);
        }
    }
}
