using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Adapter để dùng với WebView plugin:
/// - Nhận callback URL hiện tại từ WebView
/// - Nếu URL trùng VNPay return path thì gọi event đóng WebView
/// - Đồng thời trigger PaymentReturnListener để refresh coin
/// </summary>
public class VNPayWebViewReturnHandler : MonoBehaviour
{
    [SerializeField] private string vnpayReturnPath = "/api/payment/vnpay-return";
    [SerializeField] private PaymentReturnListener paymentReturnListener;
    [SerializeField] private UnityEvent onCloseBrowser;

    private string lastHandledUrl;

    /// <summary>
    /// Gọi hàm này từ callback URL-changed của WebView plugin.
    /// </summary>
    public void OnWebViewUrlChanged(string currentUrl)
    {
        if (string.IsNullOrEmpty(currentUrl))
        {
            return;
        }

        if (!IsVnPayReturnUrl(currentUrl))
        {
            return;
        }

        if (string.Equals(lastHandledUrl, currentUrl, System.StringComparison.Ordinal))
        {
            return;
        }

        lastHandledUrl = currentUrl;

        onCloseBrowser?.Invoke();

        if (paymentReturnListener != null)
        {
            paymentReturnListener.HandleReturnUrl(currentUrl);
        }
        else
        {
            Debug.LogWarning("[VNPayWebViewReturnHandler] PaymentReturnListener is missing, triggering fallback sync.");
            PaymentReturnListener listener = FindObjectOfType<PaymentReturnListener>();
            if (listener != null)
            {
                listener.HandleReturnUrl(currentUrl);
            }
        }
    }

    private bool IsVnPayReturnUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(vnpayReturnPath))
        {
            return false;
        }

        if (System.Uri.TryCreate(url, System.UriKind.Absolute, out System.Uri parsedUri))
        {
            return parsedUri.AbsolutePath.Contains(vnpayReturnPath, System.StringComparison.OrdinalIgnoreCase);
        }

        // Fallback cho URL không parse được nhưng vẫn cần match mềm.
        return url.Contains(vnpayReturnPath, System.StringComparison.OrdinalIgnoreCase);
    }
}


