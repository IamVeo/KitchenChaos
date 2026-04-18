using System;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryManager : MonoBehaviour {

    public event EventHandler OnRecipeSuccess;
    public event EventHandler OnRecipeFailed;
    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;

    public class RecipeOrder {
        public Enemy enemy;
        public RecipeSO recipeSO;
    }

    private List<RecipeOrder> waitingOrderList = new List<RecipeOrder>();

    public static DeliveryManager Instance { get; private set; }

    private int successfulRecipesAmount;

    private void Awake() {
        Instance = this;
        successfulRecipesAmount = 0;
    }

    public void AddSuccessfulDelivery(Enemy enemy, RecipeSO recipeSO)
    {
        successfulRecipesAmount++;
        OnRecipeSuccess?.Invoke(this, EventArgs.Empty);

        if (recipeSO == null)
        {
            Debug.LogError("[DeliveryManager] RecipeSO is null. Cannot resolve delivery reward from SO.");
            return;
        }

        int rewardCoin = Mathf.Max(0, recipeSO.deliveryRewardCoin);
        int rewardScore = Mathf.Max(0, recipeSO.deliveryRewardScore);

        string eventId = BuildDeliveryEventId(enemy, recipeSO, true);
        QueueOrderReward(eventId, rewardCoin, rewardScore);
    }

    public void AddFailedDelivery(Enemy enemy, RecipeSO recipeSO)
    {
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
    }

    public void AddWaitingRecipe(Enemy enemy, RecipeSO recipe) {
        waitingOrderList.Add(new RecipeOrder { enemy = enemy, recipeSO = recipe });
        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveWaitingRecipe(Enemy enemy) {
        for (int i = 0; i < waitingOrderList.Count; i++) {
            if (waitingOrderList[i].enemy == enemy) {
                waitingOrderList.RemoveAt(i);
                OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
                return;
            }
        }
    }

    public List<RecipeOrder> GetWaitingOrderList() {
        return waitingOrderList;
    }

    public bool IsRecipeMatching(PlateKitchenObject plateKitchenObject, RecipeSO waitingRecipeSO) {

        if (waitingRecipeSO.kitchenObjectSOList.Count == plateKitchenObject.GetKitchenObjectSOList().Count) {

            bool plateContentsMatchesRecipe = true;

            foreach (KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList) {
                bool ingredientFound = false;

                foreach (KitchenObjectSO plateKitchenObjectSO in plateKitchenObject.GetKitchenObjectSOList()) {
                    if (plateKitchenObjectSO == recipeKitchenObjectSO) {
                        ingredientFound = true;
                        break;
                    }
                }

                if (!ingredientFound) {
                    plateContentsMatchesRecipe = false;
                    break;
                }
            }

            return plateContentsMatchesRecipe;
        }

        return false;
    }

    public int GetSuccessfulRecipesAmount() {
        return successfulRecipesAmount;
    }

    private string BuildDeliveryEventId(Enemy enemy, RecipeSO recipeSO, bool success)
    {
        string runId = KitchenGameManager.Instance != null ? KitchenGameManager.Instance.CurrentRunId : "no_run";
        string enemyToken = enemy != null ? enemy.GetInstanceID().ToString() : "no_enemy";
        string recipeToken = recipeSO != null ? recipeSO.name : "no_recipe";
        string outcomeToken = success ? "success" : "failed";
        return $"delivery_{runId}_{enemyToken}_{recipeToken}_{successfulRecipesAmount}_{outcomeToken}";
    }

    private void QueueOrderReward(string eventId, int rewardCoin, int rewardScore)
    {
        if (DeferredRewardManager.Instance == null)
        {
            return;
        }

        DeferredRewardManager.Instance.QueueReward(
            Mathf.Max(0, rewardCoin),
            Mathf.Max(0, rewardScore),
            eventId);
    }
}
