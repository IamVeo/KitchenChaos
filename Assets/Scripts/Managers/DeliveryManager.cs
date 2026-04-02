using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryManager : MonoBehaviour {

    public event EventHandler OnRecipeSuccess;
    public event EventHandler OnRecipeFailed;
    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;

    private List<RecipeSO> waitingRecipeSOList = new List<RecipeSO>();

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

    public void AddWaitingRecipe(RecipeSO recipe) {
        waitingRecipeSOList.Add(recipe);
        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveWaitingRecipe(RecipeSO recipe) {
        waitingRecipeSOList.Remove(recipe);
        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
    }

    public List<RecipeSO> GetWaitingRecipeSOList() {
        return waitingRecipeSOList;
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
