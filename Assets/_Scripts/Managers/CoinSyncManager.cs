using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CoinSyncManager : MonoBehaviour
{
    private const string PendingDeltaKeyPrefix = "KC_Coin_Pending_";
    private const string PendingDeltaAnonymousKey = "KC_Coin_Pending_Anonymous";

    private static CoinSyncManager _instance;

    [SerializeField] private string backendBaseUrl = "http://159.89.200.36:8080/api/coins/delta";
    [SerializeField] private int requestTimeout = 20;
    [SerializeField] private float syncCheckInterval = 5f;
    [SerializeField] private float maxBackoffSeconds = 60f;
    [SerializeField] private bool enableDebugLog = true;

    private bool isSyncInProgress;
    private float nextCheckTime;
    private float nextAllowedSyncTime;
    private int consecutiveFailures;

    public static CoinSyncManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CoinSyncManager>();
                if (_instance == null)
                {
                    GameObject managerObject = new GameObject("CoinSyncManager");
                    _instance = managerObject.AddComponent<CoinSyncManager>();
                    DontDestroyOnLoad(managerObject);
                }
            }

            return _instance;
        }
    }

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

    private void Update()
    {
        if (Time.unscaledTime < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.unscaledTime + syncCheckInterval;
        TrySyncIfReady();
    }

    public void TriggerSyncNow()
    {
        nextCheckTime = 0f;
    }

    public static bool QueuePendingDelta(int delta)
    {
        if (delta == 0)
        {
            return false;
        }

        string username = ResolveUsernameForPending();
        string pendingKey = BuildPendingDeltaKey(username);
        int existingPending = PlayerPrefs.GetInt(pendingKey, 0);
        int updatedPending = existingPending + delta;

        if (updatedPending == 0)
        {
            PlayerPrefs.DeleteKey(pendingKey);
        }
        else
        {
            PlayerPrefs.SetInt(pendingKey, updatedPending);
        }

        PlayerPrefs.Save();
        Instance.TriggerSyncNow();
        return true;
    }

    public void OnUserAuthenticated(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        MigrateAnonymousPending(username);
        TriggerSyncNow();
    }

    private void TrySyncIfReady()
    {
        if (isSyncInProgress)
        {
            return;
        }

        if (!IsNetworkAvailable())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(AuthManager.JwtToken))
        {
            return;
        }

        string username = ResolveUsernameForPending();
        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        int pendingDelta = GetPendingDelta(username);
        if (pendingDelta == 0)
        {
            return;
        }

        if (Time.unscaledTime < nextAllowedSyncTime)
        {
            return;
        }

        StartCoroutine(SyncCoinDeltaCoroutine(username, pendingDelta));
    }

    private IEnumerator SyncCoinDeltaCoroutine(string username, int pendingDelta)
    {
        isSyncInProgress = true;

        UpdateCoinDeltaRequest payload = new UpdateCoinDeltaRequest { delta = pendingDelta };
        string jsonPayload = JsonUtility.ToJson(payload);

        using (UnityWebRequest request = new UnityWebRequest(backendBaseUrl, UnityWebRequest.kHttpVerbPUT))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = requestTimeout;

            request.SetRequestHeader("Authorization", $"Bearer {AuthManager.JwtToken}");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                HandleFailure($"Network/Protocol error {request.responseCode}: {request.error}");
                isSyncInProgress = false;
                yield break;
            }

            if (request.responseCode == 401 || request.responseCode == 403)
            {
                HandleFailure($"Auth error {request.responseCode}: {request.downloadHandler.text}");
                isSyncInProgress = false;
                yield break;
            }

            if (request.responseCode == 400)
            {
                DebugLog($"[CoinSync] Invalid delta payload: {request.downloadHandler.text}");
                ClearPendingDelta(username);
                isSyncInProgress = false;
                yield break;
            }

            if (request.responseCode != 200)
            {
                HandleFailure($"HTTP {request.responseCode}: {request.downloadHandler.text}");
                isSyncInProgress = false;
                yield break;
            }

            UpdateCoinDeltaResponse response = null;
            try
            {
                response = JsonUtility.FromJson<UpdateCoinDeltaResponse>(request.downloadHandler.text);
            }
            catch (Exception ex)
            {
                HandleFailure($"JSON parse error: {ex.Message}");
                isSyncInProgress = false;
                yield break;
            }

            if (response != null)
            {
                int newPending = pendingDelta - response.deltaApplied;
                if (newPending == 0)
                {
                    ClearPendingDelta(username);
                }
                else
                {
                    SetPendingDelta(username, newPending);
                }

                CurrencyManager.Instance.SetBalance(CurrencyType.Coin, response.currentCoins, false);
            }

            ResetBackoff();
            DebugLog($"[CoinSync] Sync success. deltaApplied={(response != null ? response.deltaApplied : 0)}");
        }

        isSyncInProgress = false;
    }

    private void HandleFailure(string reason)
    {
        DebugLog($"[CoinSync] Sync failed: {reason}");
        consecutiveFailures++;
        float backoffSeconds = Mathf.Min(maxBackoffSeconds, Mathf.Pow(2f, consecutiveFailures));
        nextAllowedSyncTime = Time.unscaledTime + backoffSeconds;
    }

    private void ResetBackoff()
    {
        consecutiveFailures = 0;
        nextAllowedSyncTime = 0f;
    }

    private static string ResolveUsernameForPending()
    {
        if (!string.IsNullOrWhiteSpace(AuthManager.CurrentUsername))
        {
            return AuthManager.CurrentUsername;
        }

        string lastUsername = PlayerPrefs.GetString(AuthManager.LastUsernamePrefsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(lastUsername))
        {
            return lastUsername;
        }

        return string.Empty;
    }

    private static string BuildPendingDeltaKey(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return PendingDeltaAnonymousKey;
        }

        return PendingDeltaKeyPrefix + username;
    }

    private static int GetPendingDelta(string username)
    {
        return PlayerPrefs.GetInt(BuildPendingDeltaKey(username), 0);
    }

    private static void SetPendingDelta(string username, int delta)
    {
        PlayerPrefs.SetInt(BuildPendingDeltaKey(username), delta);
        PlayerPrefs.Save();
    }

    private static void ClearPendingDelta(string username)
    {
        PlayerPrefs.DeleteKey(BuildPendingDeltaKey(username));
        PlayerPrefs.Save();
    }

    private static void MigrateAnonymousPending(string username)
    {
        int anonymousPending = PlayerPrefs.GetInt(PendingDeltaAnonymousKey, 0);
        if (anonymousPending == 0)
        {
            return;
        }

        string userKey = BuildPendingDeltaKey(username);
        int userPending = PlayerPrefs.GetInt(userKey, 0);
        PlayerPrefs.SetInt(userKey, userPending + anonymousPending);
        PlayerPrefs.DeleteKey(PendingDeltaAnonymousKey);
        PlayerPrefs.Save();
    }

    private static bool IsNetworkAvailable()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log(message);
        }
    }
}

