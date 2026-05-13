using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour {

    [Header("UI Elements")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private GameObject shopUIGameObject;

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
    }
}