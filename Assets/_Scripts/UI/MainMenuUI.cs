using System.Collections;
using System.Collections.Generic;
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
        if (string.IsNullOrEmpty(AuthManager.JwtToken)) {
            loginButton.gameObject.SetActive(true);
            logoutButton.gameObject.SetActive(false);
        } else {
            loginButton.gameObject.SetActive(false);
            logoutButton.gameObject.SetActive(true);
        }
    }
}