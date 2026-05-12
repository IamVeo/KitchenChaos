using System.Collections;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
using TMPro;

public class AuthManager : MonoBehaviour {

    [Header("API Settings")]
    private string baseUrl = "http://159.89.200.36:8080/api/auth";
    public static string JwtToken { get; private set; }
    public static string CurrentUsername { get; private set; }
    public const string LastUsernamePrefsKey = "KC_LastUsername";

    [Header("UI Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;

    [Header("Login UI References")]
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private TextMeshProUGUI loginFeedbackText;

    [Header("Register UI References")]
    [SerializeField] private TMP_InputField registerUsernameInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField registerConfirmPasswordInput;
    [SerializeField] private TextMeshProUGUI registerFeedbackText;

    private void Start() {
        // Nếu token đã có sẵn (ví dụ quay lại scene), đồng bộ cho PaymentManager.
        if (!string.IsNullOrEmpty(JwtToken)) {
            PaymentManager.Instance.SetUserToken(JwtToken);
        }

        ShowLoginPanel();
    }

    public void ShowLoginPanel() {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        loginFeedbackText.text = "";
    }

    public void ShowRegisterPanel() {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        registerFeedbackText.text = "";
    }

    public void OnLoginButtonClicked() {

        string username = loginUsernameInput.text;
        string password = loginPasswordInput.text;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) {
            loginFeedbackText.text = "Username and password cannot be empty.";
            loginFeedbackText.color = Color.red;
            return;
        }

        StartCoroutine(SendAuthRequest(username, password, "/login", true, loginFeedbackText));
    }

    public void OnRegisterButtonClicked() {
        string username = registerUsernameInput.text;
        string password = registerPasswordInput.text;
        string confirmPassword = registerConfirmPasswordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) {
            registerFeedbackText.text = "Username and password cannot be empty.";
            registerFeedbackText.color = Color.red;
            return;
        }

        if (password != confirmPassword) {
            registerFeedbackText.text = "Confirm password not match";
            registerFeedbackText.color = Color.red;
            return;
        }

        StartCoroutine(SendAuthRequest(username, password, "/register", false, registerFeedbackText));
    }

    public static void Logout() {
        JwtToken = null;
        CurrentUsername = null;

        // Đồng bộ trạng thái logout cho PaymentManager.
        PaymentManager existingPaymentManager = Object.FindObjectOfType<PaymentManager>();
        if (existingPaymentManager != null) {
            existingPaymentManager.SetUserToken(null);
        }
    }

    private IEnumerator SendAuthRequest(string username, string password, string endpoint, bool isLogin, TextMeshProUGUI targetFeedbackText) {
        targetFeedbackText.text = "Processing...";
        targetFeedbackText.color = Color.yellow;

        //Create request body
        AuthRequest requestPayload = new AuthRequest { username = username, password = password };
        string jsonPayload = JsonUtility.ToJson(requestPayload);

        //Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(baseUrl + endpoint, "POST")) {

            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            //Send request and wait for response
            yield return request.SendWebRequest();

            //Handle response
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError) {
                try {
                    MessageResponse errorResponse = JsonUtility.FromJson<MessageResponse>(request.downloadHandler.text);
                    targetFeedbackText.text = errorResponse != null && !string.IsNullOrEmpty(errorResponse.message)
                        ? errorResponse.message
                        : "Error: " + request.responseCode;
                }
                catch {
                    targetFeedbackText.text = "Cannot connect to server";
                }
                targetFeedbackText.color = Color.red;
            } else {
                if (isLogin) {
                    JwtResponse jwtResponse = JsonUtility.FromJson<JwtResponse>(request.downloadHandler.text);

                    if (jwtResponse == null || string.IsNullOrEmpty(jwtResponse.token)) {
                        targetFeedbackText.text = "Login failed: invalid token response.";
                        targetFeedbackText.color = Color.red;
                        yield break;
                    }

                    JwtToken = jwtResponse.token;
                    CurrentUsername = jwtResponse.username;
                    PlayerPrefs.SetString(LastUsernamePrefsKey, CurrentUsername ?? string.Empty);
                    PlayerPrefs.Save();
                    PaymentManager.Instance.SetUserToken(JwtToken);
                    HighScoreSyncManager.Instance.OnUserAuthenticated(CurrentUsername);

                    targetFeedbackText.text = $"Welcome, {jwtResponse.username}!";
                    targetFeedbackText.color = Color.green;

                    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainMenuScene");
                } else {
                    MessageResponse messageResponse = JsonUtility.FromJson<MessageResponse>(request.downloadHandler.text);
                    targetFeedbackText.text = messageResponse.message;
                    targetFeedbackText.color = Color.green;

                    Invoke(nameof(ShowLoginPanel), 2f);
                }
            }
        }
    }
}
