using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn lên button trong Shop UI để gọi tạo payment VNPay.
/// Script chỉ xử lý mở luồng thanh toán; coin sẽ được đồng bộ bởi PaymentReturnListener khi user quay lại app.
/// </summary>
public class VnPayShopPurchaseButton : MonoBehaviour
{
    [Header("Package")]
    [SerializeField] private long amountVnd = 50000;
    [SerializeField] private string orderInfo = "Nap tien cho game KitchenChaos";

    [Header("UI")]
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI packageNameText;
    [SerializeField] private TextMeshProUGUI coinAmountText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image packageIconImage;

    [Header("Behavior")]
    [SerializeField] private bool disableButtonWhileRequesting = true;

    private bool isRequesting;

    public TopUpPackageSo CurrentPackage { get; private set; }

    private void Awake()
    {
        if (purchaseButton == null)
        {
            purchaseButton = GetComponent<Button>();
        }
    }

    public void OnPurchaseButtonClicked()
    {
        if (isRequesting)
        {
            return;
        }

        if (string.IsNullOrEmpty(PaymentManager.Instance.CurrentUserToken))
        {
            SetStatus("Please login before purchasing coins.", true);
            return;
        }

        isRequesting = true;
        SetButtonInteractable(false);
        SetStatus("Creating VNPay payment...", false);

        if (CurrentPackage != null && !string.IsNullOrWhiteSpace(CurrentPackage.id))
        {
            PaymentManager.Instance.CreatePayment(
                CurrentPackage.id,
                onSuccess: OnCreatePaymentSuccess,
                onError: OnCreatePaymentError);
            return;
        }

        PaymentManager.Instance.CreatePayment(
            amountVnd,
            orderInfo,
            onSuccess: OnCreatePaymentSuccess,
            onError: OnCreatePaymentError);
    }

    /// <summary>
    /// Bind package data from ScriptableObject for multi-package UI.
    /// </summary>
    public void BindPackage(TopUpPackageSo packageData)
    {
        CurrentPackage = packageData;

        if (packageData == null)
        {
            return;
        }

        amountVnd = packageData.amountVnd;
        orderInfo = string.IsNullOrWhiteSpace(packageData.orderInfo)
            ? $"Nap {packageData.coinAmount} coin"
            : packageData.orderInfo;

        if (packageNameText != null)
        {
            packageNameText.text = string.IsNullOrWhiteSpace(packageData.itemName)
                ? packageData.name
                : packageData.itemName;
        }

        if (coinAmountText != null)
        {
            coinAmountText.text = $"{packageData.coinAmount} coin";
        }

        if (priceText != null)
        {
            priceText.text = $"{packageData.amountVnd:N0} VND";
        }

        if (packageIconImage != null)
        {
            packageIconImage.enabled = packageData.icon != null;
            packageIconImage.sprite = packageData.icon;
        }
    }

    private void OnCreatePaymentSuccess(string paymentUrl)
    {
        isRequesting = false;
        SetButtonInteractable(true);
        SetStatus("Redirecting to VNPay...", false);
        Debug.Log($"[VnPayShopPurchaseButton] Payment URL created: {paymentUrl}");
    }

    private void OnCreatePaymentError(string error)
    {
        isRequesting = false;
        SetButtonInteractable(true);
        SetStatus(error, true);
        Debug.LogError($"[VnPayShopPurchaseButton] Create payment failed: {error}");
    }

    private void SetButtonInteractable(bool isInteractable)
    {
        if (disableButtonWhileRequesting && purchaseButton != null)
        {
            purchaseButton.interactable = isInteractable;
        }
    }

    private void SetStatus(string message, bool isError)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message;
        statusText.color = isError ? Color.red : Color.white;
    }
}


