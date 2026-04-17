using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ví dụ về cách sử dụng PaymentManager trong một UI Script Shop
/// Đây là một mẫu tham khảo, bạn có thể tùy chỉnh theo nhu cầu của dự án
/// </summary>
public class ShopUIPaymentExample : MonoBehaviour
{
    // ============================================
    // EXAMPLE: Cách sử dụng PaymentManager
    // ============================================

    /// <summary>
    /// Phương thức được gọi khi người dùng nhấn vào một gói coin trong cửa hàng
    /// </summary>
    public void OnBuyCoinPackage(long priceVnd, string packageName)
    {
        // 1. Lấy PaymentManager Singleton
        PaymentManager paymentManager = PaymentManager.Instance;

        // 2. Nếu chưa set token, lấy token từ AuthManager hoặc nơi khác
        if (string.IsNullOrEmpty(paymentManager.GetCurrentUserToken()))
        {
            // Ví dụ: AuthManager cũng là Singleton
            // paymentManager.SetUserToken(AuthManager.Instance.GetCurrentToken());
            
            Debug.LogError("[ShopUIPaymentExample] No user token available. Please login first.");
            return;
        }

        // 3. Gọi CreatePayment với callback
        string orderInfo = $"Mua gói coin: {packageName}";
        
        paymentManager.CreatePayment(
            amount: priceVnd,
            orderInfo: orderInfo,
            onSuccess: OnPaymentUrlCreatedSuccess,
            onError: OnPaymentError
        );
    }

    /// <summary>
    /// Callback khi tạo payment URL thành công
    /// PaymentManager đã tự động mở URL trong trình duyệt
    /// </summary>
    private void OnPaymentUrlCreatedSuccess(string paymentUrl)
    {
        Debug.Log($"[ShopUIPaymentExample] Payment URL opened: {paymentUrl}");
        // Bạn có thể hiển thị thông báo cho người dùng
        // Ví dụ: ShowNotification("Đang chuyển hướng tới VNPay...");
    }

    /// <summary>
    /// Callback khi có lỗi
    /// </summary>
    private void OnPaymentError(string errorMessage)
    {
        Debug.LogError($"[ShopUIPaymentExample] Payment error: {errorMessage}");
        // Hiển thị thông báo lỗi cho người dùng trong UI
        // Ví dụ: ShowErrorDialog(errorMessage);
    }

    /// <summary>
    /// Phương thức để lấy lịch sử giao dịch và hiển thị trong UI
    /// </summary>
    public void LoadPaymentHistory()
    {
        PaymentManager paymentManager = PaymentManager.Instance;

        if (string.IsNullOrEmpty(paymentManager.GetCurrentUserToken()))
        {
            Debug.LogError("[ShopUIPaymentExample] No user token available. Please login first.");
            return;
        }

        // Gọi GetPaymentHistory
        paymentManager.GetPaymentHistory(
            limit: 20,
            onSuccess: OnPaymentHistoryLoaded,
            onError: OnPaymentHistoryError
        );
    }

    /// <summary>
    /// Callback khi lấy lịch sử giao dịch thành công
    /// </summary>
    private void OnPaymentHistoryLoaded(List<PaymentHistoryResponse> history)
    {
        Debug.Log($"[ShopUIPaymentExample] Payment history loaded. Total: {history.Count} transactions");

        foreach (var transaction in history)
        {
            string status = transaction.status;
            long amount = transaction.amountVnd;
            int coins = transaction.coinAmount;
            string date = transaction.createdAt;

            Debug.Log($"  - TxnRef: {transaction.txnRef}, Amount: {amount} VND, Coins: {coins}, Status: {status}, Date: {date}");
        }

        // Cập nhật UI với danh sách giao dịch
        // UpdateTransactionUI(history);
    }

    /// <summary>
    /// Callback khi có lỗi lấy lịch sử
    /// </summary>
    private void OnPaymentHistoryError(string errorMessage)
    {
        Debug.LogError($"[ShopUIPaymentExample] Failed to load payment history: {errorMessage}");
    }

    // ============================================
    // HƯỚNG DẪN SỬ DỤNG
    // ============================================

    /*
     * BƯỚC 1: Thiết lập Token
     * ========================
     * Sau khi người dùng đăng nhập thành công, gọi:
     *     PaymentManager.Instance.SetUserToken(jwtToken);
     *
     * BƯỚC 2: Tạo Payment URL
     * =======================
     * Khi người dùng nhấn nút "Mua" trên gói coin:
     *     OnBuyCoinPackage(50000, "50k Coins Package");
     *
     * BƯỚC 3: Xử lý Return từ VNPay
     * ==============================
     * Khi người dùng hoàn thành thanh toán trên VNPay, browser sẽ redirect tới:
     *     /api/payment/vnpay-return
     * 
     * Nếu sử dụng Deep Linking hoặc WebView trong Unity, bạn cần intercept URL này
     * để đóng browser/WebView và cập nhật UI.
     *
     * BƯỚC 4: Cập nhật Coin Balance
     * =============================
     * Sau khi nhận được thông báo thành công từ VNPay, hãy:
     * - Làm mới dữ liệu người dùng từ backend (gọi API lấy balance)
     * - Cập nhật UI hiển thị số coin mới
     * - Tùy chọn: Gọi LoadPaymentHistory() để cập nhật lịch sử giao dịch
     */
}

