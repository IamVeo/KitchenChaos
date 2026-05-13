using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string publicTop5Url = "http://159.89.200.36:8080/api/public/high-scores/top-5";
    [SerializeField] private string meUrl = "http://159.89.200.36:8080/api/high-scores/me";
    [SerializeField] private int requestTimeout = 10;
    [SerializeField] private bool enableDebugLog;

    [Header("Root Panels")]
    [SerializeField] private GameObject onlineRoot;
    [SerializeField] private GameObject offlineRoot;

    [Header("Top 5 List")]
    [SerializeField] private Transform top5Container;
    [SerializeField] private LeaderboardRowUI top5RowTemplate;

    [Header("Player Row")]
    [SerializeField] private LeaderboardRowUI playerRow;

    [Header("Offline UI")]
    [SerializeField] private TextMeshProUGUI offlineScoreText;
    
    private List<LeaderboardRowUI> currentTop5Rows = new List<LeaderboardRowUI>();
    private Coroutine refreshCoroutine;

    private void OnEnable()
    {
        RefreshLeaderboard();
    }

    public void RefreshLeaderboard()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
        }

        refreshCoroutine = StartCoroutine(RefreshCoroutine());
    }

    private IEnumerator RefreshCoroutine()
    {
        if (!IsOnlineAndAuthenticated())
        {
            RenderOffline();
            yield break;
        }

        UnityWebRequest top5Request = null;
        UnityWebRequest meRequest = null;

        try
        {
            top5Request = CreateGetRequest(publicTop5Url, false);
            meRequest = CreateGetRequest(meUrl, true);

            var top5Op = top5Request.SendWebRequest();
            var meOp = meRequest.SendWebRequest();

            while (!top5Op.isDone || !meOp.isDone)
            {
                yield return null;
            }

            if (HasRequestError(top5Request) || HasRequestError(meRequest))
            {
                DebugLog("[Leaderboard] Failed to load online data.");
                RenderOffline();
                yield break;
            }

            TopHighScoreEntry[] top5 = ParseTop5(top5Request.downloadHandler.text);
            HighScoreMeResponse me = ParseMe(meRequest.downloadHandler.text);

            if (top5 == null || me == null)
            {
                DebugLog("[Leaderboard] Invalid response payload.");
                RenderOffline();
                yield break;
            }

            RenderOnline(top5, me);
        }
        finally
        {
            top5Request?.Dispose();
            meRequest?.Dispose();
        }
    }

    private void RenderOnline(TopHighScoreEntry[] top5, HighScoreMeResponse me)
    {
        ClearTop5Rows();
        SetActiveSafe(onlineRoot, true);
        SetActiveSafe(offlineRoot, false);

        if (top5RowTemplate != null)
        {
            top5RowTemplate.gameObject.SetActive(false);
        }

        for (int i = 0; i < top5.Length; i++)
        {
            LeaderboardRowUI row = Instantiate(top5RowTemplate, top5Container);
            row.gameObject.SetActive(true);
            row.SetData(i + 1, top5[i].username, top5[i].highScore, false);
            currentTop5Rows.Add(row);
        }

        if (playerRow != null)
        {
            playerRow.SetData(me.rank, me.username, me.highScore, true);
        }
    }

    private void RenderOffline()
    {
        ClearTop5Rows();
        SetActiveSafe(onlineRoot, false);
        SetActiveSafe(offlineRoot, true);

        if (offlineScoreText != null)
        {
            offlineScoreText.text = $"High Score: {RunScoreManager.Instance.GetHighScore()}";
        }

        if (playerRow != null)
        {
            playerRow.SetData(0, AuthManager.CurrentUsername, RunScoreManager.Instance.GetHighScore(), true);
        }
    }

    private void ClearTop5Rows()
    {
        for (int i = 0; i < currentTop5Rows.Count; i++)
        {
            if (currentTop5Rows[i] != null)
            {
                Destroy(currentTop5Rows[i].gameObject);
            }
        }

        currentTop5Rows.Clear();
    }

    private UnityWebRequest CreateGetRequest(string url, bool includeAuth)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = requestTimeout;
        request.SetRequestHeader("Accept", "application/json");

        if (includeAuth)
        {
            request.SetRequestHeader("Authorization", $"Bearer {AuthManager.JwtToken}");
        }

        return request;
    }

    private bool IsOnlineAndAuthenticated()
    {
        return Application.internetReachability != NetworkReachability.NotReachable
            && !string.IsNullOrWhiteSpace(AuthManager.JwtToken);
    }

    private bool HasRequestError(UnityWebRequest request)
    {
        return request.result == UnityWebRequest.Result.ConnectionError
            || request.result == UnityWebRequest.Result.ProtocolError;
    }

    private TopHighScoreEntry[] ParseTop5(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<TopHighScoreEntry>();
        }

        string wrapped = $"{{\"items\":{json}}}";
        TopHighScoreList wrapper = JsonUtility.FromJson<TopHighScoreList>(wrapped);
        return wrapper != null && wrapper.items != null ? wrapper.items : Array.Empty<TopHighScoreEntry>();
    }

    private HighScoreMeResponse ParseMe(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonUtility.FromJson<HighScoreMeResponse>(json);
    }

    private void SetActiveSafe(GameObject target, bool value)
    {
        if (target != null)
        {
            target.SetActive(value);
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log(message);
        }
    }

    [Serializable]
    private class TopHighScoreList
    {
        public TopHighScoreEntry[] items;
    }
    
    public void OnBackButtonClicked()
    {
        gameObject.SetActive(false);
    }
}

