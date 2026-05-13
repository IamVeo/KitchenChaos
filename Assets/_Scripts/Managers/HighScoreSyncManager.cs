using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class HighScoreSyncManager : MonoBehaviour
{
    private const string PendingScoreKeyPrefix = "KC_HighScore_Pending_";
    private const string LastSyncedScoreKeyPrefix = "KC_HighScore_LastSynced_";

    private static HighScoreSyncManager _instance;

    [SerializeField] private string backendBaseUrl = "http://159.89.200.36:8080/api/high-scores";
    [SerializeField] private int requestTimeout = 20;
    [SerializeField] private float syncCheckInterval = 3f;
    [SerializeField] private float maxBackoffSeconds = 60f;
    [SerializeField] private bool enableDebugLog = true;

    private bool isSyncInProgress;
    private float nextCheckTime;
    private float nextAllowedSyncTime;
    private int consecutiveFailures;

    public static HighScoreSyncManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<HighScoreSyncManager>();
                if (_instance == null)
                {
                    GameObject managerObject = new GameObject("HighScoreSyncManager");
                    _instance = managerObject.AddComponent<HighScoreSyncManager>();
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

    public static bool QueuePendingHighScoreForCurrentUser(int score)
    {
        if (score <= 0)
        {
            return false;
        }

        string username = ResolveUsernameForPending();
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        string pendingKey = BuildPendingScoreKey(username);
        int existingPending = PlayerPrefs.GetInt(pendingKey, 0);
        if (score <= existingPending)
        {
            return false;
        }

        PlayerPrefs.SetInt(pendingKey, score);
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

        string username = AuthManager.CurrentUsername;
        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        int pendingScore = GetPendingScore(username);
        if (pendingScore <= 0)
        {
            return;
        }

        if (Time.unscaledTime < nextAllowedSyncTime)
        {
            return;
        }

        StartCoroutine(SyncHighScoreCoroutine(username, pendingScore));
    }

    private IEnumerator SyncHighScoreCoroutine(string username, int pendingScore)
    {
        isSyncInProgress = true;

        HighScoreUpdateRequest payload = new HighScoreUpdateRequest { score = pendingScore };
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
                DebugLog($"[HighScoreSync] Invalid score payload: {request.downloadHandler.text}");
                ClearPendingScore(username);
                isSyncInProgress = false;
                yield break;
            }

            if (request.responseCode != 200)
            {
                HandleFailure($"HTTP {request.responseCode}: {request.downloadHandler.text}");
                isSyncInProgress = false;
                yield break;
            }

            HighScoreUpdateResponse response = null;
            try
            {
                response = JsonUtility.FromJson<HighScoreUpdateResponse>(request.downloadHandler.text);
            }
            catch (Exception ex)
            {
                HandleFailure($"JSON parse error: {ex.Message}");
                isSyncInProgress = false;
                yield break;
            }

            if (response != null && response.currentHighScore >= 0)
            {
                RunScoreManager.Instance.SetHighScoreFromServer(response.currentHighScore);
                SetLastSyncedScore(username, response.currentHighScore);
            }

            ClearPendingScore(username);
            ResetBackoff();
            DebugLog($"[HighScoreSync] Sync success. updated={response != null && response.updated}");
        }

        isSyncInProgress = false;
    }

    private void HandleFailure(string reason)
    {
        DebugLog($"[HighScoreSync] Sync failed: {reason}");
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

        return PlayerPrefs.GetString(AuthManager.LastUsernamePrefsKey, string.Empty);
    }

    private static string BuildPendingScoreKey(string username)
    {
        return PendingScoreKeyPrefix + username;
    }

    private static string BuildLastSyncedScoreKey(string username)
    {
        return LastSyncedScoreKeyPrefix + username;
    }

    private static int GetPendingScore(string username)
    {
        return PlayerPrefs.GetInt(BuildPendingScoreKey(username), 0);
    }

    private static void ClearPendingScore(string username)
    {
        PlayerPrefs.DeleteKey(BuildPendingScoreKey(username));
        PlayerPrefs.Save();
    }

    private static void SetLastSyncedScore(string username, int score)
    {
        PlayerPrefs.SetInt(BuildLastSyncedScoreKey(username), score);
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

