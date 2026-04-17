using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lắng nghe luồng quay lại app sau khi thanh toán VNPay, sau đó đồng bộ coin từ payment history.
///
/// Lưu ý: nếu dùng Application.OpenURL (trình duyệt ngoài), Unity không thể tự đóng browser.
/// Script này xử lý phần "về lại app + refresh coin".
/// </summary>
public class PaymentReturnListener : MonoBehaviour
{
    [Header("Return Detection")]
    [SerializeField] private string vnpayReturnPath = "/api/payment/vnpay-return";
    [SerializeField] private float minSyncIntervalSeconds = 1.5f;

    [Header("History Sync")]
    [SerializeField] private int historyLimit = 20;
    [SerializeField] private bool bootstrapExistingTransactions = true;
    [SerializeField] private bool syncOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;

    private const string PROCESSED_TXN_REFS_PREFS_KEY = "Payment_ProcessedTxnRefs";
    private const string BOOTSTRAP_DONE_PREFS_KEY = "Payment_BootstrapDone";

    private readonly HashSet<string> processedTxnRefs = new HashSet<string>();
    private bool isSyncing;
    private float lastSyncTimestamp;

    private void Awake()
    {
        LoadProcessedRefs();
    }

    private void OnEnable()
    {
        Application.deepLinkActivated += Application_DeepLinkActivated;
    }

    private void OnDisable()
    {
        Application.deepLinkActivated -= Application_DeepLinkActivated;
    }

    private void Start()
    {
        if (syncOnStart)
        {
            TriggerSync("start");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // Fallback cho trường hợp quay lại app từ browser ngoài nhưng không có deep-link event.
        if (hasFocus)
        {
            TriggerSync("focus");
        }
    }

    private void Application_DeepLinkActivated(string deepLinkUrl)
    {
        HandleReturnUrl(deepLinkUrl, "deep-link");
    }

    /// <summary>
    /// Cho phép script bên ngoài (ví dụ WebView callback) chuyển URL return vào để xử lý.
    /// </summary>
    public void HandleReturnUrl(string url)
    {
        HandleReturnUrl(url, "external-callback");
    }

    public void ForceSyncNow()
    {
        TriggerSync("force");
    }

    private void HandleReturnUrl(string url, string reason)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        if (!IsVnPayReturnUrl(url))
        {
            return;
        }

        DebugLog($"[PaymentReturnListener] VNPay return detected: {url}");
        TriggerSync(reason);
    }

    private bool IsVnPayReturnUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(vnpayReturnPath))
        {
            return false;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri parsedUri))
        {
            return parsedUri.AbsolutePath.Contains(vnpayReturnPath, StringComparison.OrdinalIgnoreCase);
        }

        return url.Contains(vnpayReturnPath, StringComparison.OrdinalIgnoreCase);
    }

    private void TriggerSync(string reason)
    {
        if (isSyncing)
        {
            return;
        }

        if (Time.unscaledTime - lastSyncTimestamp < minSyncIntervalSeconds)
        {
            return;
        }

        if (string.IsNullOrEmpty(PaymentManager.Instance.CurrentUserToken))
        {
            DebugLog("[PaymentReturnListener] Skip sync: user has no token.");
            return;
        }

        lastSyncTimestamp = Time.unscaledTime;
        StartCoroutine(SyncPaymentHistoryCoroutine(reason));
    }

    private IEnumerator SyncPaymentHistoryCoroutine(string reason)
    {
        isSyncing = true;
        bool done = false;
        List<PaymentHistoryResponse> history = null;
        string error = null;

        DebugLog($"[PaymentReturnListener] Sync started. Reason: {reason}");

        PaymentManager.Instance.GetPaymentHistory(
            historyLimit,
            result =>
            {
                history = result;
                done = true;
            },
            err =>
            {
                error = err;
                done = true;
            });

        while (!done)
        {
            yield return null;
        }

        if (!string.IsNullOrEmpty(error))
        {
            DebugLog($"[PaymentReturnListener] Sync failed: {error}");
            isSyncing = false;
            yield break;
        }

        if (history == null)
        {
            DebugLog("[PaymentReturnListener] Sync completed with null history.");
            isSyncing = false;
            yield break;
        }

        bool bootstrapDone = PlayerPrefs.GetInt(BOOTSTRAP_DONE_PREFS_KEY, 0) == 1;
        if (bootstrapExistingTransactions && !bootstrapDone)
        {
            foreach (PaymentHistoryResponse transaction in history)
            {
                if (IsSuccessful(transaction))
                {
                    processedTxnRefs.Add(GetTransactionRefKey(transaction));
                }
            }

            SaveProcessedRefs();
            PlayerPrefs.SetInt(BOOTSTRAP_DONE_PREFS_KEY, 1);
            PlayerPrefs.Save();

            DebugLog("[PaymentReturnListener] Bootstrap completed. Existing successful transactions were marked as processed.");
            isSyncing = false;
            yield break;
        }

        int coinsToAdd = 0;
        foreach (PaymentHistoryResponse transaction in history)
        {
            if (!IsSuccessful(transaction))
            {
                continue;
            }

            string refKey = GetTransactionRefKey(transaction);
            if (processedTxnRefs.Contains(refKey))
            {
                continue;
            }

            processedTxnRefs.Add(refKey);
            if (transaction.coinAmount > 0)
            {
                coinsToAdd += transaction.coinAmount;
            }
        }

        if (coinsToAdd > 0)
        {
            CurrencyManager.Instance.AddCurrency(CurrencyType.Coin, coinsToAdd);
            DebugLog($"[PaymentReturnListener] Added {coinsToAdd} coin(s) from new successful payment(s).");
        }
        else
        {
            DebugLog("[PaymentReturnListener] No new successful payments to apply.");
        }

        SaveProcessedRefs();
        isSyncing = false;
    }

    private static bool IsSuccessful(PaymentHistoryResponse transaction)
    {
        return transaction != null
               && !string.IsNullOrEmpty(transaction.status)
               && transaction.status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetTransactionRefKey(PaymentHistoryResponse transaction)
    {
        if (transaction == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(transaction.txnRef))
        {
            return transaction.txnRef;
        }

        // Fallback key nếu backend không trả txnRef.
        return $"{transaction.createdAt}_{transaction.amountVnd}_{transaction.coinAmount}";
    }

    private void LoadProcessedRefs()
    {
        processedTxnRefs.Clear();

        string raw = PlayerPrefs.GetString(PROCESSED_TXN_REFS_PREFS_KEY, string.Empty);
        if (string.IsNullOrEmpty(raw))
        {
            return;
        }

        string[] refs = raw.Split('|');
        foreach (string refItem in refs)
        {
            if (!string.IsNullOrEmpty(refItem))
            {
                processedTxnRefs.Add(refItem);
            }
        }
    }

    private void SaveProcessedRefs()
    {
        string raw = string.Join("|", processedTxnRefs);
        PlayerPrefs.SetString(PROCESSED_TXN_REFS_PREFS_KEY, raw);
        PlayerPrefs.Save();
    }

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log(message);
        }
    }
}




