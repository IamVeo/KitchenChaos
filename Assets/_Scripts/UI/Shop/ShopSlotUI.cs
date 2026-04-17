using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button purchaseButton;
    
    private ShopItemSO shopItemSo;
    private TopUpPackageSo topUpPackageSo;
    private bool isTopUpSlot;
    private bool isRequestingTopUp;

    public void InitSlot(ShopItemSO item)
    {
        isTopUpSlot = false;
        isRequestingTopUp = false;
        topUpPackageSo = null;
        shopItemSo = item;
        itemNameText.text = item.itemName;
        priceText.text = item.price.ToString();
        iconImage.sprite = item.icon;
        iconImage.enabled = item.icon != null;
        
        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
        purchaseButton.interactable = true;
    }

    public void InitTopUpSlot(TopUpPackageSo packageData)
    {
        isTopUpSlot = true;
        isRequestingTopUp = false;
        shopItemSo = null;
        topUpPackageSo = packageData;

        if (topUpPackageSo == null)
        {
            gameObject.SetActive(false);
            return;
        }

        itemNameText.text = $"{topUpPackageSo.coinAmount} Coins";
        priceText.text = $"{topUpPackageSo.amountVnd:N0} VND";
        iconImage.sprite = topUpPackageSo.icon;
        iconImage.enabled = topUpPackageSo.icon != null;

        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
        purchaseButton.interactable = true;
    }
    
    private void OnPurchaseButtonClicked()
    {
        if (isTopUpSlot)
        {
            OnTopUpButtonClicked();
            return;
        }

        if (shopItemSo == null)
        {
            return;
        }

        ShopManager.Instance.AttemptPurchase(shopItemSo.id);
    }

    private void OnTopUpButtonClicked()
    {
        if (topUpPackageSo == null || isRequestingTopUp)
        {
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("[ShopSlotUI] Cannot create payment: no network connection.");
            ShopUI shopUi = FindObjectOfType<ShopUI>();
            if (shopUi != null)
            {
                shopUi.ShowCoinsNoConnectionMessage();
            }
            return;
        }

        if (string.IsNullOrEmpty(PaymentManager.Instance.CurrentUserToken))
        {
            Debug.LogWarning("[ShopSlotUI] Please login before purchasing coins.");
            return;
        }

        isRequestingTopUp = true;
        purchaseButton.interactable = false;

        if (!string.IsNullOrWhiteSpace(topUpPackageSo.id))
        {
            PaymentManager.Instance.CreatePayment(
                topUpPackageSo.id,
                onSuccess: _ =>
                {
                    isRequestingTopUp = false;
                    purchaseButton.interactable = true;
                },
                onError: error =>
                {
                    isRequestingTopUp = false;
                    purchaseButton.interactable = true;
                    Debug.LogError($"[ShopSlotUI] Create payment by packageId failed: {error}");
                });
            return;
        }

        string orderInfo = string.IsNullOrWhiteSpace(topUpPackageSo.orderInfo)
            ? $"Nap {topUpPackageSo.coinAmount} coin"
            : topUpPackageSo.orderInfo;

        PaymentManager.Instance.CreatePayment(
            topUpPackageSo.amountVnd,
            orderInfo,
            onSuccess: _ =>
            {
                isRequestingTopUp = false;
                purchaseButton.interactable = true;
            },
            onError: error =>
            {
                isRequestingTopUp = false;
                purchaseButton.interactable = true;
                Debug.LogError($"[ShopSlotUI] Create payment failed: {error}");
            });
    }
}
