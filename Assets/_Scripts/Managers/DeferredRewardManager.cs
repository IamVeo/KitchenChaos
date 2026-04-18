using System;
using System.Collections.Generic;
using UnityEngine;

public class DeferredRewardManager : MonoBehaviour
{
    public static DeferredRewardManager Instance { get; private set; }

    public event Action<DeferredRewardTotals, RunEndReason> OnRewardsCommitted;

    public DeferredRewardTotals LastCommittedTotals => lastCommittedTotals;
    public bool HasLastCommittedTotals => hasLastCommittedTotals;

    private readonly List<DeferredRewardEntry> pendingEntries = new();
    private readonly HashSet<string> processedEventIds = new();
    private readonly HashSet<string> committedRunIds = new();

    private string currentRunId;
    private bool isCommittedForCurrentRun;
    private bool hasStartedObservedRun;
    private DeferredRewardTotals lastCommittedTotals;
    private bool hasLastCommittedTotals;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (KitchenGameManager.Instance != null)
        {
            KitchenGameManager.Instance.OnRunStarted += KitchenGameManager_OnRunStarted;
            KitchenGameManager.Instance.OnRunEnded += KitchenGameManager_OnRunEnded;
        }
    }

    private void OnDestroy()
    {
        if (KitchenGameManager.Instance != null)
        {
            KitchenGameManager.Instance.OnRunStarted -= KitchenGameManager_OnRunStarted;
            KitchenGameManager.Instance.OnRunEnded -= KitchenGameManager_OnRunEnded;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void BeginRun(string runId)
    {
        currentRunId = string.IsNullOrWhiteSpace(runId) ? Guid.NewGuid().ToString("N") : runId;
        isCommittedForCurrentRun = false;
        hasStartedObservedRun = true;
        pendingEntries.Clear();
        processedEventIds.Clear();
        lastCommittedTotals = DeferredRewardTotals.Empty();
        hasLastCommittedTotals = false;

        RunScoreManager.Instance.BeginRun(currentRunId);
    }

    public bool QueueReward(int amountCoin, int amountScore, string eventId)
    {
        if (!hasStartedObservedRun || string.IsNullOrWhiteSpace(currentRunId))
        {
            return false;
        }

        if (isCommittedForCurrentRun)
        {
            return false;
        }

        if (amountCoin <= 0 && amountScore <= 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(eventId))
        {
            eventId = Guid.NewGuid().ToString("N");
        }

        if (!processedEventIds.Add(eventId))
        {
            return false;
        }

        DeferredRewardEntry entry = DeferredRewardEntry.Create(
            Mathf.Max(0, amountCoin),
            Mathf.Max(0, amountScore));

        pendingEntries.Add(entry);

        return true;
    }

    public DeferredRewardTotals GetPendingTotals()
    {
        DeferredRewardTotals totals = DeferredRewardTotals.Empty();

        foreach (DeferredRewardEntry entry in pendingEntries)
        {
            totals.totalCoin += entry.coinAmount;
            totals.totalScore += entry.scoreAmount;
        }

        return totals;
    }

    public bool CommitPendingRewardsOnGameOver()
    {
        return CommitPendingRewards(RunEndReason.GameOver);
    }

    private bool CommitPendingRewards(RunEndReason reason)
    {
        if (isCommittedForCurrentRun)
        {
            return false;
        }

        if (reason != RunEndReason.GameOver)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(currentRunId) || committedRunIds.Contains(currentRunId))
        {
            return false;
        }

        DeferredRewardTotals totals = GetPendingTotals();

        if (totals.totalCoin > 0)
        {
            CurrencyManager.Instance.AddCurrency(CurrencyType.Coin, totals.totalCoin);
        }

        if (totals.totalScore > 0)
        {
            RunScoreManager.Instance.AddScore(totals.totalScore);
        }

        RunScoreManager.Instance.FinalizeRunOnGameOver();

        Debug.Log($"[DeferredReward] GameOver summary - Score: {RunScoreManager.Instance.GetCurrentRunScore()}, HighScore: {RunScoreManager.Instance.GetHighScore()}, EarnedCoin: {totals.totalCoin}");

        isCommittedForCurrentRun = true;
        committedRunIds.Add(currentRunId);
        lastCommittedTotals = totals;
        hasLastCommittedTotals = true;
        OnRewardsCommitted?.Invoke(totals, reason);

        pendingEntries.Clear();

        return true;
    }

    private void KitchenGameManager_OnRunStarted(object sender, KitchenGameManager.RunStartedEventArgs e)
    {
        BeginRun(e.runId);
    }

    private void KitchenGameManager_OnRunEnded(object sender, KitchenGameManager.RunEndedEventArgs e)
    {
        if (e.reason != RunEndReason.GameOver)
        {
            return;
        }

        CommitPendingRewardsOnGameOver();
        hasStartedObservedRun = false;
    }
}
