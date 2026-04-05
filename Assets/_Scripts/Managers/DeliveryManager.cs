using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

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

    public void AddSuccessfulDelivery() {
        successfulRecipesAmount++;
        OnRecipeSuccess?.Invoke(this, EventArgs.Empty);
    }

    public void AddFailedDelivery() {
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

}
