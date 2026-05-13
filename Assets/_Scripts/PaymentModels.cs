using System;
using System.Collections.Generic;

/// <summary>
/// DTO để nhận URL thanh toán VNPay từ backend
/// Tương ứng với PaymentUrlResponse từ Spring Boot
/// </summary>
[Serializable]
public class PaymentUrlResponse
{
    /// <summary>
    /// URL checkout VNPay Sandbox để người dùng chuyển hướng tới
    /// </summary>
    public string url;

    // New backend may return paymentUrl instead of url.
    public string paymentUrl;

    // New backend includes transaction reference for tracking pending payments.
    public string txnRef;
}

[Serializable]
public class CreatePaymentRequest
{
    public string packageId;
}

[Serializable]
public class TopUpPackageResponse
{
    public string id;
    public string displayName;
    public int coinAmount;
    public long amountVnd;
    public string orderInfo;
    public string iconKey;
    public bool isActive;
    public int sortOrder;
}

[Serializable]
public class TopUpPackageListResponse
{
    public List<TopUpPackageResponse> data;

    public TopUpPackageListResponse()
    {
        data = new List<TopUpPackageResponse>();
    }
}

/// <summary>
/// DTO đại diện cho một giao dịch thanh toán trong lịch sử
/// Tương ứng với PaymentHistoryResponse từ Spring Boot
/// </summary>
[Serializable]
public class PaymentHistoryResponse
{
    /// <summary>
    /// Mã tham chiếu giao dịch từ VNPay
    /// </summary>
    public string txnRef;

    /// <summary>
    /// Số tiền thanh toán tính bằng VND
    /// </summary>
    public long amountVnd;

    /// <summary>
    /// Số coin trong game tương ứng với số tiền thanh toán
    /// </summary>
    public int coinAmount;

    /// <summary>
    /// Trạng thái thanh toán: SUCCESS, FAILED, PENDING
    /// </summary>
    public string status;

    /// <summary>
    /// Mã response từ VNPay
    /// </summary>
    public string vnpResponseCode;

    /// <summary>
    /// Thời gian tạo giao dịch (ISO 8601 format)
    /// </summary>
    public string createdAt;

    /// <summary>
    /// Thời gian giao dịch được thanh toán thành công (ISO 8601 format)
    /// </summary>
    public string paidAt;
}

/// <summary>
/// Wrapper cho list PaymentHistoryResponse để hỗ trợ deserialization từ JSON array
/// </summary>
[Serializable]
public class PaymentHistoryList
{
    public List<PaymentHistoryResponse> data;

    public PaymentHistoryList()
    {
        data = new List<PaymentHistoryResponse>();
    }
}
