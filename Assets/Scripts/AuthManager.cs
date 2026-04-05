using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
using TMPro;

public class AuthManager : MonoBehaviour {

    [Header("API Settings")]
    private string baseUrl = "http://159.89.200.36:8080/api/auth";
    public static string JwtToken { get; private set; }

    [Header("UI Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("Login UI References")]
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;
    public TextMeshProUGUI loginFeedbackText;

    [Header("Register UI References")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerConfirmPasswordInput;
    public TextMeshProUGUI registerFeedbackText;

    private void Start() {
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

    public void Logout() {
        JwtToken = null;
        UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
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
                    JwtToken = jwtResponse.token;

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
