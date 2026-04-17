using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// PaymentManager quản lý các yêu cầu thanh toán VNPay từ Unity client tới backend.
/// Sử dụng Singleton pattern để dễ dàng gọi từ các script UI khác.
/// 
/// Sử dụng UnityWebRequest để thực hiện HTTP GET requests.
/// Hỗ trợ Authentication bằng JWT token.
/// </summary>
public class PaymentManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    private static PaymentManager _instance;

    /// <summary>
    /// Base URL của backend. Có thể thiết lập thông qua Inspector hoặc code
    /// </summary>
    [SerializeField]
    private string backendBaseUrl = "http://159.89.200.36:8080/api/payment";

    [SerializeField]
    private string topUpPackagesUrl = "http://159.89.200.36:8080/api/topup-packages";

    /// <summary>
    /// JWT token của người dùng hiện tại
    /// Được gán từ AuthManager hoặc trực tiếp
    /// </summary>
    private string currentUserToken;

    /// <summary>
    /// Timeout cho mỗi request (tính bằng giây)
    /// </summary>
    [SerializeField]
    private int requestTimeout = 30;

    /// <summary>
    /// Flag để kiểm soát xem có logging hay không (dùng cho debug)
    /// </summary>
    [SerializeField]
    private bool enableDebugLog = true;

    public static PaymentManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Tìm PaymentManager trong scene
                _instance = FindObjectOfType<PaymentManager>();

                // Nếu không tìm thấy, tạo một GameObject mới
                if (_instance == null)
                {
                    GameObject paymentManagerObject = new GameObject("PaymentManager");
                    _instance = paymentManagerObject.AddComponent<PaymentManager>();
                    DontDestroyOnLoad(paymentManagerObject);
                    Debug.Log("[PaymentManager] Singleton instance created.");
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// JWT token hiện tại. Có thể set trực tiếp từ script đăng nhập.
    /// </summary>
    public string CurrentUserToken
    {
        get => currentUserToken;
        set => currentUserToken = value;
    }

    private void Awake()
    {
        // Đảm bảo chỉ có một instance duy nhất
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;
    }

    /// <summary>
    /// Thiết lập JWT token cho các request yêu cầu authentication
    /// </summary>
    /// <param name="token">JWT token từ backend (thường được lưu sau khi login)</param>
    public void SetUserToken(string token)
    {
        currentUserToken = token;

        if (string.IsNullOrEmpty(token))
        {
            DebugLog("[PaymentManager] User token was cleared.");
            return;
        }

        DebugLog($"[PaymentManager] User token set: {token.Substring(0, Math.Min(20, token.Length))}...");
    }

    /// <summary>
    /// Lấy token hiện tại
    /// </summary>
    public string GetCurrentUserToken()
    {
        return currentUserToken;
    }

    /// <summary>
    /// Tạo URL thanh toán VNPay từ backend và mở trong trình duyệt
    /// </summary>
    /// <param name="amount">Số tiền tính bằng VND (mặc định: 50000 nếu truyền 0 hoặc âm)</param>
    /// <param name="onSuccess">Callback khi thành công, truyền URL VNPay</param>
    /// <param name="onError">Callback khi lỗi, truyền thông báo lỗi</param>
    public void CreatePayment(long amount, Action<string> onSuccess, Action<string> onError)
    {
        CreatePayment(amount, "Nap tien cho game KitchenChaos", onSuccess, onError);
    }

    /// <summary>
    /// API mới: tạo payment theo packageId từ backend.
    /// </summary>
    public void CreatePayment(string packageId, Action<string> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(currentUserToken))
        {
            string errorMsg = "[PaymentManager] No user token set. Please call SetUserToken() first.";
            DebugLog(errorMsg);
            onError?.Invoke(errorMsg);
            return;
        }

        if (string.IsNullOrWhiteSpace(packageId))
        {
            string errorMsg = "[PaymentManager] packageId is required for create-payment API.";
            DebugLog(errorMsg);
            onError?.Invoke(errorMsg);
            return;
        }

        StartCoroutine(CreatePaymentByPackageCoroutine(packageId, onSuccess, onError));
    }

    /// <summary>
    /// Overload cho phép truyền orderInfo tùy chọn.
    /// </summary>
    public void CreatePayment(long amount, string orderInfo, Action<string> onSuccess, Action<string> onError)
    {
        // Validate token
        if (string.IsNullOrEmpty(currentUserToken))
        {
            string errorMsg = "[PaymentManager] No user token set. Please call SetUserToken() first.";
            DebugLog(errorMsg);
            onError?.Invoke(errorMsg);
            return;
        }

        // Nếu amount <= 0, sử dụng giá trị mặc định
        if (amount <= 0)
        {
            amount = 50000;
        }

        // Nếu orderInfo trống, sử dụng mặc định
        if (string.IsNullOrWhiteSpace(orderInfo))
        {
            orderInfo = "Nap tien cho game KitchenChaos";
        }

        // Bắt đầu coroutine gọi API
        StartCoroutine(CreatePaymentCoroutine(amount, orderInfo, onSuccess, onError));
    }

    /// <summary>
    /// Coroutine thực hiện HTTP GET request để tạo payment URL
    /// </summary>
    private IEnumerator CreatePaymentCoroutine(long amount, string orderInfo, Action<string> onSuccess, Action<string> onError)
    {
        // Xây dựng URL với query parameters
        string url = $"{backendBaseUrl}/create-payment?amount={amount}&orderInfo={UnityWebRequest.EscapeURL(orderInfo)}";
        
        DebugLog($"Creating payment request to: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Thiết lập Authorization header
            request.SetRequestHeader("Authorization", $"Bearer {currentUserToken}");
            request.SetRequestHeader("Accept", "application/json");

            // Thiết lập timeout
            request.timeout = requestTimeout;

            // Gửi request
            yield return request.SendWebRequest();

            // Xử lý response
            if (request.result == UnityWebRequest.Result.ConnectionError || 
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                string responseBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                string errorMsg = $"[PaymentManager] Network/Protocol Error ({request.responseCode}): {request.error}. Body: {responseBody}";
                DebugLog(errorMsg);
                onError?.Invoke(errorMsg);
            }
            else if (request.responseCode == 200)
            {
                try
                {
                    // Parse JSON response
                    string jsonResponse = request.downloadHandler.text;
                    DebugLog($"Payment URL response: {jsonResponse}");

                    PaymentUrlResponse paymentResponse = JsonUtility.FromJson<PaymentUrlResponse>(jsonResponse);

                    string paymentUrl = ExtractPaymentUrl(paymentResponse);
                    if (!string.IsNullOrEmpty(paymentUrl))
                    {
                        DebugLog($"Payment URL created successfully. Opening: {paymentUrl}");
                        
                        // Gọi callback thành công
                        onSuccess?.Invoke(paymentUrl);

                        // Mở URL trong trình duyệt mặc định của thiết bị
                        Application.OpenURL(paymentUrl);
                    }
                    else
                    {
                        string errorMsg = "[PaymentManager] Payment URL is empty in response";
                        DebugLog(errorMsg);
                        onError?.Invoke(errorMsg);
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = $"[PaymentManager] JSON Parse Error: {ex.Message}";
                    DebugLog(errorMsg);
                    onError?.Invoke(errorMsg);
                }
            }
            else
            {
                string errorMsg = $"[PaymentManager] HTTP Error {request.responseCode}: {request.downloadHandler.text}";
                DebugLog(errorMsg);
                onError?.Invoke(errorMsg);
            }
        }
    }

    private IEnumerator CreatePaymentByPackageCoroutine(string packageId, Action<string> onSuccess, Action<string> onError)
    {
        string url = $"{backendBaseUrl}/create-payment";
        CreatePaymentRequest payload = new CreatePaymentRequest { packageId = packageId };
        string jsonPayload = JsonUtility.ToJson(payload);

        DebugLog($"Creating payment request by packageId to: {url}. packageId: {packageId}");

        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = requestTimeout;

            request.SetRequestHeader("Authorization", $"Bearer {currentUserToken}");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                string responseBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                string errorMsg = $"[PaymentManager] CreatePaymentByPackage failed ({request.responseCode}): {request.error}. Body: {responseBody}";
                DebugLog(errorMsg);
                onError?.Invoke(errorMsg);
                yield break;
            }

            if (request.responseCode != 200)
            {
                string errorMsg = $"[PaymentManager] HTTP Error {request.responseCode}: {request.downloadHandler.text}";
                DebugLog(errorMsg);
                onError?.Invoke(errorMsg);
                yield break;
            }

            try
            {
                string jsonResponse = request.downloadHandler.text;
                DebugLog($"CreatePaymentByPackage response: {jsonResponse}");

                PaymentUrlResponse paymentResponse = JsonUtility.FromJson<PaymentUrlResponse>(jsonResponse);
                string paymentUrl = ExtractPaymentUrl(paymentResponse);

                if (string.IsNullOrEmpty(paymentUrl))
                {
                    string errorMsg = "[PaymentManager] Payment URL is empty in create-payment response.";
                    DebugLog(errorMsg);
                    onError?.Invoke(errorMsg);
                    yield break;
                }

                onSuccess?.Invoke(paymentUrl);
                Application.OpenURL(paymentUrl);
            }
            catch (Exception ex)
            {
                string errorMsg = $"[PaymentManager] JSON Parse Error: {ex.Message}";
                DebugLog(errorMsg);
                onError?.Invoke(errorMsg);
            }
        }
    }

    public void GetTopUpPackages(Action<List<TopUpPackageResponse>> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetTopUpPackagesCoroutine(onSuccess, onError));
    }

    private IEnumerator GetTopUpPackagesCoroutine(Action<List<TopUpPackageResponse>> onSuccess, Action<string> onError)
    {
        DebugLog($"Fetching top-up packages from: {topUpPackagesUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(topUpPackagesUrl))
        {
            if (!string.IsNullOrEmpty(currentUserToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {currentUserToken}");
            }
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = requestTimeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                string responseBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                string errorMsg = $"[PaymentManager] GetTopUpPackages failed ({request.responseCode}): {request.error}. Body: {responseBody}";
                DebugLog(errorMsg);
                onError?.Invoke(errorMsg);
                yield break;
            }

            if (request.responseCode != 200)
            {
                string errorMsg = $"[PaymentManager] HTTP Error {request.responseCode}: {request.downloadHandler.text}";
                DebugLog(errorMsg);
                onError?.Invoke(errorMsg);
                yield break;
            }

            string jsonResponse = request.downloadHandler.text;

            try
            {
                List<TopUpPackageResponse> packageList = ParseTopUpPackageList(jsonResponse);
                onSuccess?.Invoke(packageList);
            }
            catch (Exception ex)
            {
                string errorMsg = $"[PaymentManager] Failed to parse top-up packages: {ex.Message}. Body: {jsonResponse}";
                DebugLog(errorMsg);
                onError?.Invoke(errorMsg);
            }
        }
    }

    /// <summary>
    /// Lấy lịch sử giao dịch thanh toán từ backend
    /// </summary>
    /// <param name="limit">Số lượng ghi nhận tối đa cần lấy (mặc định: 20)</param>
    /// <param name="onSuccess">Callback khi thành công, truyền danh sách lịch sử</param>
    /// <param name="onError">Callback khi lỗi, truyền thông báo lỗi</param>
    public void GetPaymentHistory(int limit, Action<List<PaymentHistoryResponse>> onSuccess, Action<string> onError)
    {
        // Validate token
        if (string.IsNullOrEmpty(currentUserToken))
        {
            string errorMsg = "[PaymentManager] No user token set. Please call SetUserToken() first.";
            DebugLog(errorMsg);
            onError?.Invoke(errorMsg);
            return;
        }

        // Nếu limit <= 0, sử dụng mặc định
        if (limit <= 0)
        {
            limit = 20;
        }

        // Bắt đầu coroutine gọi API
        StartCoroutine(GetPaymentHistoryCoroutine(limit, onSuccess, onError));
    }

    /// <summary>
    /// Coroutine thực hiện HTTP GET request để lấy payment history
    /// </summary>
    private IEnumerator GetPaymentHistoryCoroutine(int limit, Action<List<PaymentHistoryResponse>> onSuccess, Action<string> onError)
    {
        // Xây dựng URL với query parameters
        string url = $"{backendBaseUrl}/history?limit={limit}";
        
        DebugLog($"Fetching payment history from: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Thiết lập Authorization header
            request.SetRequestHeader("Authorization", $"Bearer {currentUserToken}");
            request.SetRequestHeader("Accept", "application/json");

            // Thiết lập timeout
            request.timeout = requestTimeout;

            // Gửi request
            yield return request.SendWebRequest();

            // Xử lý response
            if (request.result == UnityWebRequest.Result.ConnectionError || 
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                string responseBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                string errorMsg = $"[PaymentManager] Network/Protocol Error ({request.responseCode}): {request.error}. Body: {responseBody}";
                DebugLog(errorMsg);
                onError?.Invoke(errorMsg);
            }
            else if (request.responseCode == 200)
            {
                try
                {
                    // Parse JSON response
                    string jsonResponse = request.downloadHandler.text;
                    DebugLog($"Payment history response received: {jsonResponse.Substring(0, Math.Min(100, jsonResponse.Length))}...");

                    // Nếu response là array, wrap nó trong object
                    if (jsonResponse.StartsWith("["))
                    {
                        jsonResponse = "{\"data\":" + jsonResponse + "}";
                    }

                    PaymentHistoryList historyList = JsonUtility.FromJson<PaymentHistoryList>(jsonResponse);
                    
                    if (historyList != null && historyList.data != null)
                    {
                        DebugLog($"Payment history retrieved successfully. Total records: {historyList.data.Count}");
                        onSuccess?.Invoke(historyList.data);
                    }
                    else
                    {
                        DebugLog("Payment history is empty or null");
                        onSuccess?.Invoke(new List<PaymentHistoryResponse>());
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = $"[PaymentManager] JSON Parse Error: {ex.Message}";
                    DebugLog(errorMsg);
                    onError?.Invoke(errorMsg);
                }
            }
            else
            {
                string errorMsg = $"[PaymentManager] HTTP Error {request.responseCode}: {request.downloadHandler.text}";
                DebugLog(errorMsg);
                onError?.Invoke(errorMsg);
            }
        }
    }

    /// <summary>
    /// Thiết lập backend base URL
    /// </summary>
    public void SetBackendUrl(string url)
    {
        backendBaseUrl = url;
        DebugLog($"Backend URL set to: {backendBaseUrl}");
    }

    public void SetTopUpPackagesUrl(string url)
    {
        topUpPackagesUrl = url;
        DebugLog($"Top-up packages URL set to: {topUpPackagesUrl}");
    }

    /// <summary>
    /// Lấy backend base URL hiện tại
    /// </summary>
    public string GetBackendUrl()
    {
        return backendBaseUrl;
    }

    public string GetTopUpPackagesUrl()
    {
        return topUpPackagesUrl;
    }

    private static string ExtractPaymentUrl(PaymentUrlResponse paymentResponse)
    {
        if (paymentResponse == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(paymentResponse.paymentUrl))
        {
            return paymentResponse.paymentUrl;
        }

        return paymentResponse.url;
    }

    private static List<TopUpPackageResponse> ParseTopUpPackageList(string jsonResponse)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            return new List<TopUpPackageResponse>();
        }

        string trimmedJson = jsonResponse.TrimStart();

        if (trimmedJson.StartsWith("["))
        {
            trimmedJson = "{\"data\":" + trimmedJson + "}";
        }

        TopUpPackageListResponse response = JsonUtility.FromJson<TopUpPackageListResponse>(trimmedJson);
        if (response == null || response.data == null)
        {
            return new List<TopUpPackageResponse>();
        }

        return response.data;
    }

    /// <summary>
    /// Helper method để log debug messages
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log(message);
        }
    }
}

