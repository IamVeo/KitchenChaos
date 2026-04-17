# Hướng Dẫn Tích Hợp VNPay Payment System - Unity C#

## Tổng Quan

Các script này cho phép Unity client tương tác với backend VNPay Sandbox để xử lý thanh toán in-game coins. Hệ thống sử dụng:
- **UnityWebRequest** để gửi HTTP GET requests
- **JWT Token** để xác thực người dùng
- **Singleton Pattern** cho PaymentManager để dễ truy cập toàn cục
- **Coroutines** để xử lý async requests

---

## Các File Được Tạo

### 1. **PaymentModels.cs**
Chứa các DTO (Data Transfer Objects) tương ứng với backend responses:

- **PaymentUrlResponse**: Chứa URL VNPay checkout
  ```csharp
  {
      "url": "https://sandbox.vnpayment.vn/paygate?..."
  }
  ```

- **PaymentHistoryResponse**: Đại diện cho một giao dịch
  ```csharp
  {
      "txnRef": "TXN12345",
      "amountVnd": 50000,
      "coinAmount": 1000,
      "status": "SUCCESS",
      "vnpResponseCode": "00",
      "createdAt": "2024-04-14T10:30:00Z",
      "paidAt": "2024-04-14T10:35:00Z"
  }
  ```

### 2. **PaymentManager.cs**
Manager chính xử lý giao tiếp với backend:

**Đặc điểm:**
- Singleton pattern (tự tạo nếu chưa tồn tại)
- DontDestroyOnLoad (tồn tại giữa các scene)
- 30 giây timeout mặc định
- Debug logging hỗ trợ

**Các phương thức chính:**
- `SetUserToken(string token)`: Thiết lập JWT token
- `CreatePayment(long amount, Action<string> onSuccess, Action<string> onError)`: Tạo payment URL với orderInfo mặc định
- `CreatePayment(long amount, string orderInfo, Action<string> onSuccess, Action<string> onError)`: Tạo payment URL với orderInfo tùy chỉnh
- `GetPaymentHistory(int limit, Action<List<PaymentHistoryResponse>> onSuccess, Action<string> onError)`: Lấy lịch sử giao dịch

### 3. **ShopUIPaymentExample.cs**
Ví dụ cơ bản về cách sử dụng PaymentManager trong UI

### 4. **PaymentReturnListener.cs**
Lắng nghe lúc app quay lại từ VNPay (deep link hoặc focus) và tự đồng bộ coin từ payment history.

### 5. **VnPayShopPurchaseButton.cs**
Script gắn lên button nạp coin, gọi `PaymentManager.CreatePayment(...)` và khóa nút trong lúc request đang chạy.

### 6. **VNPayWebViewReturnHandler.cs**
Adapter cho WebView callback: detect URL return VNPay, đóng WebView qua UnityEvent và trigger refresh coin.

### 7. **TopUpPackageSo.cs + TopUpPackageListSo.cs**
Data model gói nạp bằng ScriptableObject, giúp quản lý nhiều package (50 coin, 100 coin, ... ) mà không hardcode trong button.

### 8. **TopUpPackageListUI.cs**
Spawner runtime để tạo nhiều `VnPayShopPurchaseButton` từ `TopUpPackageListSo`.

---

## Hướng Dẫn Sử Dụng Chi Tiết

### BƯỚC 1: Thiết Lập Backend URL (Nếu cần)

```csharp
PaymentManager.Instance.SetBackendUrl("http://your-backend-url:8080/api/payment");
```

Mặc định là: `http://localhost:8080/api/payment`

### BƯỚC 2: Thiết Lập JWT Token

Sau khi người dùng đăng nhập thành công, lấy token từ AuthManager hoặc response login:

```csharp
public class AuthManager : MonoBehaviour
{
    public void OnLoginSuccess(JwtResponse jwtResponse)
    {
        string token = jwtResponse.token;
        PaymentManager.Instance.SetUserToken(token);
        Debug.Log("User token set for payment requests");
    }
}
```

> Trong project hiện tại, `AuthManager` đã được gắn sẵn để tự gọi `PaymentManager.Instance.SetUserToken(JwtToken)` sau login thành công và tự clear token khi logout.

### BƯỚC 3: Tạo Payment URL Khi Người Dùng Mua Coin

```csharp
public void OnBuyCoinPackage()
{
    PaymentManager paymentManager = PaymentManager.Instance;
    
    // Kiểm tra token
    if (string.IsNullOrEmpty(paymentManager.GetCurrentUserToken()))
    {
        Debug.LogError("User not logged in!");
        return;
    }
    
    // Gọi CreatePayment
    paymentManager.CreatePayment(
        amount: 50000,  // 50,000 VND
        orderInfo: "Mua 1000 coins",
        onSuccess: (url) => {
            Debug.Log("Payment URL created: " + url);
            // PaymentManager đã tự động mở URL trong trình duyệt
        },
        onError: (error) => {
            Debug.LogError("Payment error: " + error);
            ShowErrorDialog(error);
        }
    );
}
```

### BƯỚC 4: Xử Lý Return từ VNPay

Sau khi thanh toán, VNPay redirect đến `/api/payment/vnpay-return`. Nếu sử dụng WebView hoặc Deep Linking, hãy intercept URL này:

```csharp
// Ví dụ sử dụng WebView plugin
void OnWebViewURLChanged(string url)
{
    if (url.Contains("/vnpay-return"))
    {
        // Đóng WebView
        CloseWebView();
        
        // Cập nhật coin balance
        RefreshUserBalance();
        
        // Tải lịch sử giao dịch
        LoadPaymentHistory();
    }
}
```

### BƯỚC 5: Hiển Thị Lịch Sử Giao Dịch

```csharp
public void OnShowPaymentHistory()
{
    PaymentManager.Instance.GetPaymentHistory(
        limit: 20,
        onSuccess: (historyList) => {
            Debug.Log($"Got {historyList.Count} transactions");
            
            foreach (var transaction in historyList)
            {
                Debug.Log($"{transaction.txnRef}: {transaction.amountVnd} VND - {transaction.status}");
                // Cập nhật UI với giao dịch
            }
        },
        onError: (error) => {
            Debug.LogError("Failed to load history: " + error);
        }
    );
}
```

---

## Xử Lý Lỗi

PaymentManager tự động xử lý các loại lỗi:

1. **NetworkError**: Kết nối internet bị gián đoạn
2. **ProtocolError**: Lỗi HTTP (404, 500, etc)
3. **JSON Parse Error**: Response JSON không hợp lệ
4. **Missing Token**: Người dùng chưa đăng nhập

Tất cả lỗi được truyền qua callback `onError` để bạn xử lý trong UI.

---

## Các Thiết Lập Có Thể Điều Chỉnh (Inspector)

Trong Inspector, bạn có thể thiết lập:

- **Backend Base Url**: URL của backend (mặc định: http://localhost:8080/api/payment)
- **Request Timeout**: Thời gian chờ tối đa cho mỗi request tính bằng giây (mặc định: 30)
- **Enable Debug Log**: Bật/tắt debug logging (mặc định: true)

---

## Luồng Hoàn Chỉnh

```
┌─────────────────────────────────────────────────────┐
│ 1. Người dùng nhấn "Mua Coin" trong Shop UI          │
└──────────────────┬──────────────────────────────────┘
                   │
                   ▼
        ┌──────────────────────────────┐
        │ 2. Gọi PaymentManager         │
        │    .CreatePayment()           │
        └──────────────┬────────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │ 3. HTTP GET /create-payment   │
        │    + Authorization: Bearer   │
        └──────────────┬────────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │ 4. Backend tạo URL VNPay      │
        │    Return PaymentUrlResponse  │
        └──────────────┬────────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │ 5. Mở URL VNPay trong browser │
        │    (Application.OpenURL)     │
        └──────────────┬────────────────┘
                       │
                       ▼
    ┌──────────────────────────────────────┐
    │ 6. Người dùng thanh toán trên VNPay  │
    └──────────────┬───────────────────────┘
                   │
                   ▼
    ┌──────────────────────────────────────┐
    │ 7. VNPay redirect đến vnpay-return   │
    └──────────────┬───────────────────────┘
                   │
                   ▼
    ┌──────────────────────────────────────┐
    │ 8. App intercept & đóng browser      │
    │    Refresh user balance              │
    │    Load payment history (optional)   │
    └──────────────────────────────────────┘
```

---

## Lưu Ý Quan Trọng

### ✅ Những Điều Cần Làm

1. **Luôn kiểm tra token trước khi gọi API:**
   ```csharp
   if (string.IsNullOrEmpty(PaymentManager.Instance.GetCurrentUserToken()))
   {
       // Xử lý: yêu cầu login
   }
   ```

2. **Xử lý callback onError** để hiển thị thông báo cho người dùng

3. **Cập nhật UI** sau khi thanh toán thành công (refresh coin balance)

4. **Kiểm tra mã response** từ VNPay (`vnpResponseCode: "00"` = thành công)

### ❌ Những Điều Không Nên Làm

1. Không lưu JWT token trên disk nếu không cần (hoặc mã hóa)
2. Không gọi CreatePayment nhiều lần liên tiếp
3. Không bỏ qua xử lý lỗi callback

---

## Ví Dụ Tích Hợp Với AuthManager

```csharp
public class AuthManager : MonoBehaviour
{
    private static AuthManager _instance;
    private JwtResponse _currentUser;

    public static AuthManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Login(string username, string password)
    {
        StartCoroutine(LoginCoroutine(username, password));
    }

    private IEnumerator LoginCoroutine(string username, string password)
    {
        // ... (gọi backend login API)
        // Giả sử nhận được JwtResponse
        _currentUser = new JwtResponse
        {
            token = "eyJhbGciOiJIUzI1NiIs...",
            username = username,
            id = 123
        };

        // Thiết lập token cho PaymentManager
        PaymentManager.Instance.SetUserToken(_currentUser.token);

        yield return null;
    }

    public JwtResponse GetCurrentUser() => _currentUser;
}
```

---

## Debugging

Bật Debug Log trong PaymentManager Inspector để xem chi tiết:
- Các URL request được gọi
- Request headers
- Response nhận được
- Lỗi parse JSON
- Lỗi network/protocol

---

## Triển Khai Trực Tiếp Trong `ShopTestScene`

Từ dữ liệu scene hiện tại:
- Scene đã có `ShopManager` object: `Assets/Scenes/ShopTestScene.unity`
- UI shop đang lấy từ prefab `Assets/Prefabs/UI/ShopUI.prefab`
- `ShopUI` có script `ShopUI` và nút cheat `AddCoinCheatBtn`

Thực hiện theo checklist sau:

1. **Gắn script lắng nghe return VNPay**
   - Mở prefab `Assets/Prefabs/UI/ShopUI.prefab`
   - Chọn root object `ShopUI`
   - Add Component: `PaymentReturnListener`
   - Giữ mặc định:
     - `vnpayReturnPath = /api/payment/vnpay-return`
     - `historyLimit = 20`
     - `bootstrapExistingTransactions = true`

2. **Đảm bảo có `PaymentManager` runtime**
   - Không bắt buộc đặt sẵn trong scene vì `PaymentManager.Instance` tự tạo nếu chưa có.
   - Nếu muốn chỉnh URL bằng Inspector, tạo GameObject `PaymentManager` trong scene và attach script `PaymentManager`.

3. **Tạo nút nạp coin bằng VNPay trong Shop UI**
   - Thêm một button (ví dụ `Buy50kVNPayButton`) trong prefab `ShopUI`
   - Add component `VnPayShopPurchaseButton` vào button đó
   - Set `amountVnd` theo package (ví dụ 50000)
   - (Optional) kéo `TextMeshProUGUI` vào `statusText` để hiển thị trạng thái
   - Trong `Button.onClick`, gọi `VnPayShopPurchaseButton.OnPurchaseButtonClicked()`

4. **Flow kiểm thử trong editor/device**
   - Login -> vào shop -> bấm nút nạp -> VNPay mở browser
   - Thanh toán xong quay lại app
   - `PaymentReturnListener` sẽ gọi history API và cộng coin mới vào `CurrencyManager`
   - `ShopUI` tự cập nhật text coin qua luồng `ShopManager.OnCoinChanged`

5. **Thiết lập Deep Link (khuyến nghị)**
   - Android: cấu hình intent filter để app nhận URL callback
   - iOS: cấu hình URL scheme / universal link
   - Nếu chưa có deep link, fallback focus vẫn chạy, nhưng có thể refresh chậm hơn

6. **Nếu bạn dùng WebView plugin thay vì browser ngoài**
   - Tạo object trong scene/prefab, gắn `VNPayWebViewReturnHandler`
   - Bind callback URL-changed của plugin vào `VNPayWebViewReturnHandler.OnWebViewUrlChanged(string)`
   - Gán `paymentReturnListener`
   - Gán event `onCloseBrowser` để gọi hàm đóng WebView của plugin

---

## Mô Hình Nhiều Gói Nạp (ScriptableObject)

1. Tạo assets package:
   - `Create > ScriptableObjects > Shop > TopUpPackageSo`
   - Ví dụ:
     - package A: `displayName=Goi 50 Coin`, `coinAmount=50`, `amountVnd=50000`
     - package B: `displayName=Goi 100 Coin`, `coinAmount=100`, `amountVnd=90000`

2. Tạo list asset:
   - `Create > ScriptableObjects > Shop > TopUpPackageListSo`
   - Kéo các `TopUpPackageSo` vào `packageList`

3. Chuẩn bị prefab item button:
   - Item prefab có `Button` + component `VnPayShopPurchaseButton`
   - Gán các text/icon optional (`packageNameText`, `coinAmountText`, `priceText`, `packageIconImage`)

4. Trong panel Shop, tạo object `TopUpPackageListUI`:
   - Add component `TopUpPackageListUI`
   - Gán `packageListSo`
   - Gán `itemContainer` (Grid/Vertical Layout)
   - Gán `packageButtonPrefab`

Khi chạy scene, `TopUpPackageListUI` sẽ tự instantiate button theo dữ liệu SO và mỗi button sẽ gọi `PaymentManager.CreatePayment(...)` đúng amount/orderInfo của package.

---

## Tính Năng Tương Lai (Có Thể Mở Rộng)

1. **Retry Logic**: Tự động retry khi request thất bại
2. **Caching**: Lưu cache lịch sử giao dịch cục bộ
3. **Request Queue**: Xếp hàng các request để tránh timeout
4. **WebView Integration**: Tích hợp with native WebView để xử lý VNPay return
5. **Deep Linking**: Hỗ trợ deep link để xử lý vnpay-return callback

---

## Hỗ Trợ & Troubleshooting

**Vấn đề: "No user token set"**
- Nguyên nhân: Chưa gọi `SetUserToken()` hoặc token bị lỗi
- Giải pháp: Kiểm tra flow login, đảm bảo token được truyền từ AuthManager

**Vấn đề: "HTTP Error 401"**
- Nguyên nhân: Token không hợp lệ hoặc đã hết hạn
- Giải pháp: Refresh token hoặc yêu cầu user login lại

**Vấn đề: "JSON Parse Error"**
- Nguyên nhân: Backend response format không khớp DTO
- Giải pháp: Kiểm tra format JSON từ backend

---

## Liên Hệ & Cập Nhật

Nếu cần thay đổi backend URL hoặc configuration khác, sửa trong inspector PaymentManager object hoặc gọi:

```csharp
PaymentManager.Instance.SetBackendUrl("http://new-backend-url");
```

---

**Version**: 1.0  
**Last Updated**: April 14, 2026  
**Compatibility**: Unity 2020.3+ (C# 9.0+)

