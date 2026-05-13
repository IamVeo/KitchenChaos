using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour {

    [Header("UI Elements")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button leaderboardButton;
    
    [Header("UI Popups")]
    [SerializeField] private GameObject shopUIGameObject;
    [SerializeField] private TextMeshProUGUI greetingText;
    [SerializeField] private GameObject leaderboardUIGameObject;

    private void Awake() {
        playButton.onClick.AddListener(() => {
            SceneManager.LoadSceneAsync("NewGameScene");
        });
        loginButton.onClick.AddListener(() => {
            SceneManager.LoadSceneAsync("LoginScene");
        });
        logoutButton.onClick.AddListener(() => {
            AuthManager.Logout();
            UpdateLoginStatus();
        });
        shopButton.onClick.AddListener(() => {
            shopUIGameObject.SetActive(true);
        });
        leaderboardButton.onClick.AddListener(() => {
            leaderboardUIGameObject.SetActive(true);
        });
        quitButton.onClick.AddListener(() => {
            Application.Quit();
        });

        Time.timeScale = 1f;
        shopUIGameObject.SetActive(false);
        
    }
    
    private void Start() {
        UpdateLoginStatus();
    }

    private void UpdateLoginStatus() {
        
        
        
        bool isLoggedIn = !string.IsNullOrEmpty(AuthManager.JwtToken);
        
        playButton.gameObject.SetActive(isLoggedIn);
        logoutButton.gameObject.SetActive(isLoggedIn);
        shopButton.gameObject.SetActive(isLoggedIn);
        leaderboardButton.gameObject.SetActive(isLoggedIn);
        
        loginButton.gameObject.SetActive(!isLoggedIn);
		
		 if (greetingText != null) {
            if (isLoggedIn && !string.IsNullOrEmpty(AuthManager.CurrentUsername)) {
                greetingText.gameObject.SetActive(true);
                greetingText.text = $"Greeting, {AuthManager.CurrentUsername}";
            } else {
                greetingText.gameObject.SetActive(false);
                greetingText.text = string.Empty;
            }
        }
    }

    public void SetGreetingVisible(bool isVisible) {
        if (greetingText == null) {
            return;
        }

        if (!isVisible) {
            greetingText.gameObject.SetActive(false);
            return;
        }

        UpdateLoginStatus();
    }
}