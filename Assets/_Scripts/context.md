# KitchenChaos Backend - VNPay Payment API Context

## 1. Overview
This document describes the backend REST APIs for the VNPay Sandbox payment integration. The backend is built with Spring Boot. The Unity client needs to consume these APIs to facilitate in-game currency purchases.

Note: All endpoints currently use the HTTP GET method. Endpoints interacting with user data require Authentication (passing a JWT token in the Authorization header).

## 2. DTOs (Data Transfer Objects)

### 2.1 PaymentUrlResponse
Used to receive the generated VNPay checkout URL from the backend.
- Properties:
    - `url` (String): The full VNPay Sandbox URL to redirect the user to.

### 2.2 PaymentHistoryResponse
Used to receive the history of user transactions.
- Properties:
    - `txnRef` (String): The transaction reference ID.
    - `amountVnd` (long): The payment amount in VND.
    - `coinAmount` (int): The amount of in-game coin equivalent.
    - `status` (Enum/String): The payment status (e.g., SUCCESS, FAILED, PENDING).
    - `vnpResponseCode` (String): The response code returned by VNPay.
    - `createdAt` (DateTime string): The timestamp when the transaction was created.
    - `paidAt` (DateTime string): The timestamp when the transaction was successfully paid.

## 3. API Endpoints

### 3.1 Create Payment URL
- Endpoint: `/api/payment/create-payment`
- Method: `GET`
- Auth Required: Yes (Authorization: Bearer <token>)
- Query Parameters:
    - `amount` (long, optional): The amount in VND. Default is 50000.
    - `orderInfo` (String, optional): Description of the order. Default is "Nap tien cho game KitchenChaos".
- Response: `PaymentUrlResponse` (JSON)
- Unity Usage: Call this API when the user clicks a package in the Shop Scene. Open the returned `url` in the device's browser or an in-app browser/WebView.

### 3.2 Get Payment History
- Endpoint: `/api/payment/history`
- Method: `GET`
- Auth Required: Yes (Authorization: Bearer <token>)
- Query Parameters:
    - `limit` (int, optional): The maximum number of records to return. Default is 20.
- Response: `List<PaymentHistoryResponse>` (JSON array)
- Unity Usage: Call this API to populate the transaction history UI in the Shop Scene.

### 3.3 VNPay Return URL (Frontend Callback)
- Endpoint: `/api/payment/vnpay-return`
- Method: `GET`
- Auth Required: No
- Description: This is the URL VNPay redirects the user back to after completing the payment on the VNPay gateway. It returns a simple String message ("Giao dich thanh cong...", "Chu ky khong hop le!", or "Giao dich that bai...").
- Unity Usage: If using Deep Linking or an in-app browser, the Unity app should intercept this URL redirect to close the browser, show a success/failure UI, and refresh the player's coin balance.

### 3.4 VNPay IPN (Server-to-Server Webhook)
- Endpoint: `/api/payment/vnpay-ipn`
- Method: `GET`
- Auth Required: No
- Description: Used purely by the VNPay server to notify the backend asynchronously about the transaction status.
- Unity Usage: None. The Unity client does not interact with this endpoint.

## 4. Expected Integration Flow (Unity -> Backend)
1. Player selects a coin package in the Unity Shop Scene.
2. Unity sends a GET request to `/api/payment/create-payment` with the `amount`. Include the user's auth token.
3. Unity receives the `PaymentUrlResponse` JSON and parses the `url`.
4. Unity opens the `url` using `Application.OpenURL()` or a WebView plugin.
5. Player completes the payment on the VNPay Sandbox page.
6. VNPay redirects the browser to `/api/payment/vnpay-return`.
7. Unity detects the return phase (via deep link or WebView event), closes the web interface.
8. Unity fetches the updated coin balance and optionally calls `/api/payment/history` to update the logs.