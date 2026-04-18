using System;
using TMPro;
using UnityEngine;

public class GameOverUI : MonoBehaviour {

    [SerializeField] private TextMeshProUGUI recipesDeliveredText;
    [SerializeField] private TextMeshProUGUI runScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI totalRewardText;

    private void Start() {
        if (KitchenGameManager.Instance != null)
        {
            KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
        }

        if (DeferredRewardManager.Instance != null)
        {
            DeferredRewardManager.Instance.OnRewardsCommitted += DeferredRewardManager_OnRewardsCommitted;
        }

        Hide();
    }

    private void OnDestroy()
    {
        if (KitchenGameManager.Instance != null)
        {
            KitchenGameManager.Instance.OnStateChanged -= KitchenGameManager_OnStateChanged;
        }

        if (DeferredRewardManager.Instance != null)
        {
            DeferredRewardManager.Instance.OnRewardsCommitted -= DeferredRewardManager_OnRewardsCommitted;
        }
    }

    private void KitchenGameManager_OnStateChanged(object sender, EventArgs e) {
        if (KitchenGameManager.Instance != null && KitchenGameManager.Instance.IsGameOver()) {
            Show();
            RefreshGameOverStats();
        } else {
            Hide();
        }
    }

    private void DeferredRewardManager_OnRewardsCommitted(DeferredRewardTotals totals, RunEndReason reason)
    {
        if (reason != RunEndReason.GameOver)
        {
            return;
        }

        if (gameObject.activeInHierarchy)
        {
            RefreshGameOverStats();
        }
    }

    private void RefreshGameOverStats()
    {
        if (DeliveryManager.Instance != null && recipesDeliveredText != null)
        {
            recipesDeliveredText.text = DeliveryManager.Instance.GetSuccessfulRecipesAmount().ToString();
        }

        SetOptionalText(runScoreText, $"Score: {RunScoreManager.Instance.GetCurrentRunScore()}");
        SetOptionalText(highScoreText, $"High Score: {RunScoreManager.Instance.GetHighScore()}");

        DeferredRewardTotals totals = DeferredRewardTotals.Empty();
        if (DeferredRewardManager.Instance != null && DeferredRewardManager.Instance.HasLastCommittedTotals)
        {
            totals = DeferredRewardManager.Instance.LastCommittedTotals;
        }

        SetOptionalText(totalRewardText, $"Coins earned: {totals.totalCoin}");
    }

    private static void SetOptionalText(TextMeshProUGUI textComponent, string value)
    {
        if (textComponent == null)
        {
            return;
        }

        textComponent.text = value;
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void Hide() {
        gameObject.SetActive(false);
    }


}